using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Matchmaking;
using SpeedDate.Packets.Rooms;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Matchmaker;
using GameInfoType = SpeedDate.Packets.Matchmaking.GameInfoType;

namespace SpeedDate.ServerPlugins.Rooms
{
    class RoomsPlugin : SpeedDateServerPlugin, IGamesProvider
    {
        public int RegisterRoomPermissionLevel = 0;


        private readonly Dictionary<int, RegisteredRoom> _rooms = new Dictionary<int, RegisteredRoom>();

        private int _roomIdGenerator;

        public event Action<RegisteredRoom> RoomRegistered; 
        public event Action<RegisteredRoom> RoomDestroyed;

        public override void Loaded()
        {
            // Add handlers
            Server.SetHandler((ushort)OpCodes.RegisterRoom, HandleRegisterRoom);
            Server.SetHandler((ushort)OpCodes.DestroyRoom, HandleDestroyRoom);
            Server.SetHandler((ushort)OpCodes.SaveRoomOptions, HandleSaveRoomOptions);
            Server.SetHandler((ushort)OpCodes.GetRoomAccess, HandleGetRoomAccess);
            Server.SetHandler((ushort)OpCodes.ValidateRoomAccess, HandleValidateRoomAccess);
            Server.SetHandler((ushort)OpCodes.PlayerLeftRoom, HandlePlayerLeftRoom);
            
            // Maintain unconfirmed accesses
            Task.Factory.StartNew(CleanUnconfirmedAccesses, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Returns true, if peer has permissions to register a game server
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        protected virtual bool HasRoomRegistrationPermissions(IPeer peer)
        {
            var extension = peer.GetExtension<PeerSecurityExtension>();

            return extension.PermissionLevel >= RegisterRoomPermissionLevel;
        }

        public int GenerateRoomId()
        {
            return _roomIdGenerator++;
        }

        /// <summary>
        /// Registers a room to the server
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual RegisteredRoom RegisterRoom(IPeer peer, RoomOptions options)
        {
            // Create the object
            var room = new RegisteredRoom(GenerateRoomId(), peer, options);

            var peerRooms = peer.GetProperty((int) PeerPropertyKeys.RegisteredRooms) as Dictionary<int, RegisteredRoom>;

            if (peerRooms == null)
            {
                // If this is the first time creating a room

                // Save the dictionary
                peerRooms = new Dictionary<int, RegisteredRoom>();
                peer.SetProperty((int)PeerPropertyKeys.RegisteredRooms, peerRooms);

                // Listen to disconnect event
                peer.Disconnected += OnRegisteredPeerDisconnect;
            }

            // Add a new room to peer
            peerRooms[room.RoomId] = room;

            // Add the room to a list of all rooms
            _rooms[room.RoomId] = room;

            // Invoke the event
            RoomRegistered?.Invoke(room);

            return room;
        }

        /// <summary>
        /// Unregisters a room from a server
        /// </summary>
        /// <param name="room"></param>
        public virtual void DestroyRoom(RegisteredRoom room)
        {
            var peer = room.Peer;

            if (peer != null)
            {
                var peerRooms = peer.GetProperty((int)PeerPropertyKeys.RegisteredRooms) as Dictionary<int, RegisteredRoom>;

                // Remove the room from peer
                peerRooms?.Remove(room.RoomId);
            }

            // Remove the room from all rooms
            _rooms.Remove(room.RoomId);

            room.Destroy();

            // Invoke the event
            RoomDestroyed?.Invoke(room);
        }

        private void OnRegisteredPeerDisconnect(IPeer peer)
        {
            var peerRooms = peer.GetProperty((int)PeerPropertyKeys.RegisteredRooms) as Dictionary<int, RegisteredRoom>;

            if (peerRooms == null)
                return;

            // Create a copy so that we can iterate safely
            var registeredRooms = peerRooms.Values.ToList();

            foreach (var registeredRoom in registeredRooms)
            {
                DestroyRoom(registeredRoom);
            }
        }

        public virtual void ChangeRoomOptions(RegisteredRoom room, RoomOptions options)
        {
            room.ChangeOptions(options);
        }

        private async void CleanUnconfirmedAccesses()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                foreach (var registeredRoom in _rooms.Values)
                {
                    registeredRoom.ClearTimedOutAccesses();
                }
            }
        }

        public IEnumerable<GameInfoPacket> GetPublicGames(IPeer peer, Dictionary<string, string> filters)
        {
            return _rooms.Values.Where(r => r.Options.IsPublic).Select(r => new GameInfoPacket()
            {
                Id = r.RoomId,
                Address = r.Options.RoomIp + ":" + r.Options.RoomPort,
                MaxPlayers = r.Options.MaxPlayers,
                Name = r.Options.Name,
                OnlinePlayers = r.OnlineCount,
                Properties = GetPublicRoomProperties(peer, r, filters),
                IsPasswordProtected = !string.IsNullOrEmpty(r.Options.Password),
                Type = GameInfoType.Room
            });
        }

        public virtual Dictionary<string, string> GetPublicRoomProperties(IPeer player, RegisteredRoom room, 
            Dictionary<string, string> playerFilters)
        {
            return room.Options.Properties;
        }

        public RegisteredRoom GetRoom(int roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public IEnumerable<RegisteredRoom> GetAllRooms()
        {
            return _rooms.Values;
        }

        #region Message Handlers

        private void HandleRegisterRoom(IIncommingMessage message)
        {
            if (!HasRoomRegistrationPermissions(message.Peer))
            {
                message.Respond("Insufficient permissions", ResponseStatus.Unauthorized);
                return;
            }

            var options = message.Deserialize<RoomOptions>();

            var room = RegisterRoom(message.Peer, options);

            // Respond with a room id
            message.Respond(room.RoomId, ResponseStatus.Success);
        }

        protected virtual void HandleDestroyRoom(IIncommingMessage message)
        {
            var roomId = message.AsInt();

            RegisteredRoom room;
            _rooms.TryGetValue(roomId, out room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (message.Peer != room.Peer)
            {
                // Wrong peer unregistering the room
                message.Respond("You're not the creator of the room", ResponseStatus.Unauthorized);
                return;
            }

            DestroyRoom(room);

            message.Respond(ResponseStatus.Success);
        }

        private void HandleValidateRoomAccess(IIncommingMessage message)
        {
            var data = message.Deserialize<RoomAccessValidatePacket>();

            _rooms.TryGetValue(data.RoomId, out var room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (message.Peer != room.Peer)
            {
                // Wrong peer unregistering the room
                message.Respond("You're not the creator of the room", ResponseStatus.Unauthorized);
                return;
            }

            if (!room.ValidateAccess(data.Token, out var playerPeer))
            {
                message.Respond("Failed to confirm the access", ResponseStatus.Unauthorized);
                return;
            }

            var packet = new UsernameAndPeerIdPacket()
            {
                PeerId =  playerPeer.ConnectId
            };

            // Add username if available
            var userExt = playerPeer.GetExtension<UserExtension>();
            if (userExt != null)
            {
                packet.Username = userExt.Username ?? "";
            }

            // Respond with success and player's peer id
            message.Respond(packet, ResponseStatus.Success);
        }

        protected virtual void HandleSaveRoomOptions(IIncommingMessage message)
        {
            var data = message.Deserialize<SaveRoomOptionsPacket>();

            _rooms.TryGetValue(data.RoomId, out var room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (message.Peer != room.Peer)
            {
                // Wrong peer unregistering the room
                message.Respond("You're not the creator of the room", ResponseStatus.Unauthorized);
                return;
            }

            ChangeRoomOptions(room, data.Options);

            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleGetRoomAccess(IIncommingMessage message)
        {
            var data = message.Deserialize<RoomAccessRequestPacket>();

            RegisteredRoom room;
            _rooms.TryGetValue(data.RoomId, out room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (!string.IsNullOrEmpty(room.Options.Password) && room.Options.Password != data.Password)
            {
                message.Respond("Invalid password", ResponseStatus.Unauthorized);
                return;
            }

            // Send room access request to peer who owns it
            room.GetAccess(message.Peer, data.Properties, (packet) =>
            {
                message.Respond(packet, ResponseStatus.Success);
            }, err =>
            {
                message.Respond(err, ResponseStatus.Unauthorized);
            });
        }

        private void HandlePlayerLeftRoom(IIncommingMessage message)
        {
            var data = message.Deserialize<PlayerLeftRoomPacket>();

            _rooms.TryGetValue(data.RoomId, out var room);

            if (room == null)
            {
                message.Respond("Room does not exist", ResponseStatus.Failed);
                return;
            }

            if (message.Peer != room.Peer)
            {
                // Wrong peer unregistering the room
                message.Respond("You're not the creator of the room", ResponseStatus.Unauthorized);
                return;
            }

            room.OnPlayerLeft(data.PeerId);

            message.Respond(ResponseStatus.Success);
        }

        #endregion


    }
}


