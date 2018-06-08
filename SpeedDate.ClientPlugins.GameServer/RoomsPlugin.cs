using System;
using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Rooms;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.GameServer
{
    public delegate void RoomCreationCallback(RoomController controller, string error);
    public delegate void RoomAccessValidateCallback(UsernameAndPeerIdPacket usernameAndPeerId, string error);

    public delegate void RegisterSpawnedProcessCallback(SpawnTaskController taskController, string error);
    public delegate void CompleteSpawnedProcessCallback(bool isSuccessful, string error);

    public class RoomsPlugin : SpeedDateClientPlugin
    {
        private static Dictionary<int, RoomController> _localCreatedRooms;

        /// <summary>
        /// Maximum time the master server can wait for a response from game server
        /// to see if it can give access to a peer
        /// </summary>
        public float AccessProviderTimeout = 3;

        /// <summary>
        /// Event, invoked when a room is registered
        /// </summary>
        public event Action<RoomController> RoomRegistered; 

        /// <summary>
        /// Event, invoked when a room is destroyed
        /// </summary>
        public event Action<RoomController> RoomDestroyed;

        public RoomsPlugin(IClientSocket connection) : base(connection)
        {
            _localCreatedRooms = new Dictionary<int, RoomController>();
        }

        /// <summary>
        /// Sends a request to register a room to master server
        /// </summary>
        public void RegisterRoom(RoomOptions options, RoomCreationCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            Connection.SendMessage((short) OpCodes.RegisterRoom, options, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    // Failed to register room
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                var roomId = response.AsInt();

                var controller = new RoomController(this, roomId, Connection, options);

                // Save the reference
                _localCreatedRooms[roomId] = controller;

                callback.Invoke(controller, null);

                // Invoke event
                RoomRegistered?.Invoke(controller);
            });
        }

        /// <summary>
        /// Sends a request to destroy a room of a given room id
        /// </summary>
        public void DestroyRoom(int roomId, SuccessCallback callback)
        {
            DestroyRoom(roomId, callback, Connection);
        }

        /// <summary>
        /// Sends a request to destroy a room of a given room id
        /// </summary>
        public void DestroyRoom(int roomId, SuccessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            connection.SendMessage((short)OpCodes.DestroyRoom, roomId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown Error"));
                    return;
                }

                _localCreatedRooms.TryGetValue(roomId, out var destroyedRoom);
                _localCreatedRooms.Remove(roomId);
                
                callback.Invoke(true, null);

                // Invoke event
                if (destroyedRoom != null)
                    RoomDestroyed?.Invoke(destroyedRoom);
            });
        }

        /// <summary>
        /// Sends a request to master server, to see if a given token is valid
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="token"></param>
        /// <param name="callback"></param>
        public void ValidateAccess(int roomId, string token, RoomAccessValidateCallback callback)
        {
            ValidateAccess(roomId, token, callback, Connection);
        }

        /// <summary>
        /// Sends a request to master server, to see if a given token is valid
        /// </summary>
        public void ValidateAccess(int roomId, string token, RoomAccessValidateCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            var packet = new RoomAccessValidatePacket()
            {
                RoomId = roomId,
                Token = token
            };

            connection.SendMessage((short)OpCodes.ValidateRoomAccess, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                callback.Invoke(response.Deserialize(new UsernameAndPeerIdPacket()), null);
            });
        }

        /// <summary>
        /// Updates the options of the registered room
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="options"></param>
        /// <param name="callback"></param>
        public void SaveOptions(int roomId, RoomOptions options, SuccessCallback callback)
        {
            SaveOptions(roomId, options, callback, Connection);
        }

        /// <summary>
        /// Updates the options of the registered room
        /// </summary>
        public void SaveOptions(int roomId, RoomOptions options, SuccessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            var changePacket = new SaveRoomOptionsPacket()
            {
                Options = options,
                RoomId =  roomId
            };

            connection.SendMessage((short) OpCodes.SaveRoomOptions, changePacket, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown Error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Notifies master server that a user with a given peer id has left the room
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="peerId"></param>
        /// <param name="callback"></param>
        public void NotifyPlayerLeft(int roomId, int peerId, SuccessCallback callback)
        {
            NotifyPlayerLeft(roomId, peerId, callback, Connection);
        }

        /// <summary>
        /// Notifies master server that a user with a given peer id has left the room
        /// </summary>
        public void NotifyPlayerLeft(int roomId, int peerId, SuccessCallback callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, null);
                return;
            }

            var packet = new PlayerLeftRoomPacket()
            {
                PeerId = peerId,
                RoomId = roomId
            };

            connection.SendMessage((short) OpCodes.PlayerLeftRoom, packet, (status, response) =>
            {
                callback.Invoke(status == ResponseStatus.Success, null);
            });
        }

        /// <summary>
        /// Get's a room controller (of a registered room, which was registered in current process)
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public RoomController GetRoomController(int roomId)
        {
            _localCreatedRooms.TryGetValue(roomId, out var controller);
            return controller;
        }

        /// <summary>
        /// Retrieves all of the locally created rooms (their controllers)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RoomController> GetLocallyCreatedRooms()
        {
            return _localCreatedRooms.Values;
        }


        /// <summary>
        /// This should be called from a process which is spawned.
        /// For example, it can be called from a game server, which is started by the spawner
        /// On successfull registration, callback contains <see cref="SpawnTaskController"/>, which 
        /// has a dictionary of properties, that were given when requesting a process to be spawned
        /// </summary>
        public void RegisterSpawnedProcess(int spawnId, string spawnCode, RegisterSpawnedProcessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            var packet = new RegisterSpawnedProcessPacket()
            {
                SpawnCode = spawnCode,
                SpawnId = spawnId
            };

            Connection.SendMessage((short)OpCodes.RegisterSpawnedProcess, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                var properties = new Dictionary<string, string>().FromBytes(response.AsBytes());

                var process = new SpawnTaskController(this, spawnId, properties, Connection);

                callback.Invoke(process, null);
            });
        }

        /// <summary>
        /// This method should be called, when spawn process is finalized (finished spawning).
        /// For example, when spawned game server fully starts
        /// </summary>
        public void FinalizeSpawnedProcess(int spawnId, CompleteSpawnedProcessCallback callback, Dictionary<string, string> finalizationData = null)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            var packet = new SpawnFinalizationPacket()
            {
                SpawnId = spawnId,
                FinalizationData = finalizationData ?? new Dictionary<string, string>()
            };

            Connection.SendMessage((short)OpCodes.CompleteSpawnProcess, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown Error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }
    }
}