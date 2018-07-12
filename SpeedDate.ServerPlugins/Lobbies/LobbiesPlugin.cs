using System.Collections.Generic;
using System.Linq;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Common;
using SpeedDate.Packets.Lobbies;
using SpeedDate.Packets.Matchmaking;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Matchmaker;
using SpeedDate.ServerPlugins.Rooms;
using SpeedDate.ServerPlugins.Spawner;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public class LobbiesPlugin : SpeedDateServerPlugin, IGamesProvider
    {
        [Inject] private readonly ILogger _logger;
        [Inject] internal readonly RoomsPlugin RoomsPlugin;
        [Inject] internal readonly SpawnerPlugin SpawnerPlugin;
        
        public int CreateLobbiesPermissionLevel = 0;

        protected readonly Dictionary<string, ILobbyFactory> Factories = new Dictionary<string, ILobbyFactory>();

        protected readonly Dictionary<int, ILobby> Lobbies = new Dictionary<int, ILobby>();

        public bool DontAllowCreatingIfJoined = true;
        public int JoinedLobbiesLimit = 1;

        private int _nextLobbyId;


        public override void Loaded()
        {
            Server.SetHandler((ushort)OpCodes.CreateLobby, HandleCreateLobby);
            Server.SetHandler((ushort)OpCodes.JoinLobby, HandleJoinLobby);
            Server.SetHandler((ushort)OpCodes.LeaveLobby, HandleLeaveLobby);
            Server.SetHandler((ushort)OpCodes.SetLobbyProperties, HandleSetLobbyProperties);
            Server.SetHandler((ushort)OpCodes.SetMyLobbyProperties, HandleSetMyProperties);
            Server.SetHandler((ushort)OpCodes.JoinLobbyTeam, HandleJoinTeam);
            Server.SetHandler((ushort)OpCodes.LobbySendChatMessage, HandleSendChatMessage);
            Server.SetHandler((ushort)OpCodes.LobbySetReady, HandleSetReadyStatus);
            Server.SetHandler((ushort)OpCodes.LobbyStartGame, HandleStartGame);
            Server.SetHandler((ushort)OpCodes.GetLobbyRoomAccess, HandleGetLobbyRoomAccess);

            Server.SetHandler((ushort)OpCodes.GetLobbyMemberData, HandleGetLobbyMemberData);
            Server.SetHandler((ushort)OpCodes.GetLobbyInfo, HandleGetLobbyInfo);
        }

        protected virtual bool CheckIfHasPermissionToCreate(IPeer peer)
        {
            var extension = peer.GetExtension<PeerSecurityExtension>();

            return extension.PermissionLevel >= CreateLobbiesPermissionLevel;
        }

        public void AddFactory(ILobbyFactory factory)
        {
            if (Factories.ContainsKey(factory.Id))
                _logger.Warn("You are overriding a factory with same id");

            Factories[factory.Id] = factory;
        }

        public bool AddLobby(ILobby lobby)
        {
            if (Lobbies.ContainsKey(lobby.Id))
            {
                _logger.Error("Failed to add a lobby - lobby with same id already exists");
                return false;
            }

            Lobbies.Add(lobby.Id, lobby);

            lobby.Destroyed += OnLobbyDestroyed;
            return true;
        }

        /// <summary>
        /// Invoked, when lobby is destroyed
        /// </summary>
        /// <param name="lobby"></param>
        protected virtual void OnLobbyDestroyed(ILobby lobby)
        {
            Lobbies.Remove(lobby.Id);
            lobby.Destroyed -= OnLobbyDestroyed;
        }

        protected virtual LobbyUserExtension GetOrCreateLobbiesExtension(IPeer peer)
        {
            var extension = peer.GetExtension<LobbyUserExtension>();

            if (extension == null)
            {
                extension = new LobbyUserExtension(peer);
                peer.AddExtension(extension);
            }

            return extension;
        }

        public int GenerateLobbyId()
        {
            return _nextLobbyId++;
        }

        #region Message Handlers

        protected virtual void HandleCreateLobby(IIncommingMessage message)
        {
            if (!CheckIfHasPermissionToCreate(message.Peer))
            {
                message.Respond("Insufficient permissions", ResponseStatus.Unauthorized);
                return;
            }

            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);

            if (DontAllowCreatingIfJoined && lobbiesExt.CurrentLobby != null)
            {
                // If peer is already in a lobby
                message.Respond("You are already in a lobby", ResponseStatus.Failed);
                return;
            }

            // Deserialize properties of the lobby
            var properties = new Dictionary<string, string>().FromBytes(message.AsBytes());

            if (!properties.ContainsKey(OptionKeys.LobbyFactoryId))
            {
                message.Respond("Invalid request (undefined factory)", ResponseStatus.Failed);
                return;
            }

            // Get the lobby factory
            Factories.TryGetValue(properties[OptionKeys.LobbyFactoryId], out var factory);

            if (factory == null)
            {
                message.Respond("Unavailable lobby factory", ResponseStatus.Failed);
                return;
            }

            var newLobby = factory.CreateLobby(properties, message.Peer);

            if (!AddLobby(newLobby))
            {
                message.Respond("Lobby registration failed", ResponseStatus.Error);
                return;
            }

            _logger.Info("Lobby created: " + newLobby.Id);

            // Respond with success and lobby id
            message.Respond(newLobby.Id, ResponseStatus.Success);
        }

        /// <summary>
        /// Handles a request from user to join a lobby
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleJoinLobby(IIncommingMessage message)
        {
            var user = GetOrCreateLobbiesExtension(message.Peer);

            if (user.CurrentLobby != null)
            {
                message.Respond("You're already in a lobby", ResponseStatus.Failed);
                return;
            }

            var lobbyId = message.AsInt();

            Lobbies.TryGetValue(lobbyId, out var lobby);

            if (lobby == null)
            {
                message.Respond("Lobby was not found", ResponseStatus.Failed);
                return;
            }

            if (!lobby.AddPlayer(user, error => message.Respond(error ?? "Failed to add player to lobby", ResponseStatus.Failed)))
            {
                return;
            }

            var data = lobby.GenerateLobbyData(user);

            message.Respond(data, ResponseStatus.Success);
        }

        /// <summary>
        /// Handles a request from user to leave a lobby
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleLeaveLobby(IIncommingMessage message)
        {
            var lobbyId = message.AsInt();

            Lobbies.TryGetValue(lobbyId, out var lobby);

            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);

            lobby?.RemovePlayer(lobbiesExt);

            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleSetLobbyProperties(IIncommingMessage message)
        {
            var data = message.Deserialize<LobbyPropertiesSetPacket>();

            Lobbies.TryGetValue(data.LobbyId, out var lobby);

            if (lobby == null)
            {
                message.Respond("Lobby was not found", ResponseStatus.Failed);
                return;
            }

            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);

            foreach (var dataProperty in data.Properties)
            {
                if (!lobby.SetProperty(lobbiesExt, dataProperty.Key, dataProperty.Value))
                {
                    message.Respond("Failed to set the property: " + dataProperty.Key, 
                        ResponseStatus.Failed);
                    return;
                }
            }

            message.Respond(ResponseStatus.Success);
        }

        private void HandleSetMyProperties(IIncommingMessage message)
        {
            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);

            var lobby = lobbiesExt.CurrentLobby;

            if (lobby == null)
            {
                message.Respond("Lobby was not found", ResponseStatus.Failed);
                return;
            }

            var properties = new Dictionary<string, string>().FromBytes(message.AsBytes());

            var player = lobby.GetMember(lobbiesExt);

            foreach (var dataProperty in properties)
            {
                // We don't change properties directly,
                // because we want to allow an implementation of lobby
                // to do "sanity" checking
                if (!lobby.SetPlayerProperty(player, dataProperty.Key, dataProperty.Value))
                {
                    message.Respond("Failed to set property: " + dataProperty.Key, ResponseStatus.Failed);
                    return;
                }
            }

            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleSetReadyStatus(IIncommingMessage message)
        {
            var isReady = message.AsInt() > 0;

            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);
            var lobby = lobbiesExt.CurrentLobby;

            if (lobby == null)
            {
                message.Respond("You're not in a lobby", ResponseStatus.Failed);
                return;
            }

            var member = lobby.GetMember(lobbiesExt);

            if (member == null)
            {
                message.Respond("Invalid request", ResponseStatus.Failed);
                return;
            }

            lobby.SetReadyState(member, isReady);
            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleJoinTeam(IIncommingMessage message)
        {
            var data = message.Deserialize<LobbyJoinTeamPacket>();

            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);
            var lobby = lobbiesExt.CurrentLobby;

            if (lobby == null)
            {
                message.Respond("You're not in a lobby", ResponseStatus.Failed);
                return;
            }

            var player = lobby.GetMember(lobbiesExt);

            if (player == null)
            {
                message.Respond("Invalid request", ResponseStatus.Failed);
                return;
            }

            if (!lobby.TryJoinTeam(data.TeamName, player))
            {
                message.Respond("Failed to join a team: " + data.TeamName, ResponseStatus.Failed);
                return;
            }

            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleSendChatMessage(IIncommingMessage message)
        {
            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);
            var lobby = lobbiesExt.CurrentLobby;

            var member = lobby.GetMember(lobbiesExt);

            // Invalid request
            if (member == null)
                return;

            lobby.HandleChatMessage(member, message);
        }

        protected virtual void HandleStartGame(IIncommingMessage message)
        {
            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);
            var lobby = lobbiesExt.CurrentLobby;

            if (!lobby.StartGameManually(lobbiesExt))
            {
                message.Respond("Failed starting the game", ResponseStatus.Failed);
                return;
            }

            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleGetLobbyRoomAccess(IIncommingMessage message)
        {
            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);
            var lobby = lobbiesExt.CurrentLobby;

            if (lobby == null)
            {
                message.Respond("Invalid request", ResponseStatus.Failed);
                return;
            }
            lobby.HandleGameAccessRequest(message);
        }

        protected virtual void HandleGetLobbyMemberData(IIncommingMessage message)
        {
            var data = message.Deserialize<IntPairPacket>();
            var lobbyId = data.A;
            var peerId = data.B;

            Lobbies.TryGetValue(lobbyId, out var lobby);

            if (lobby == null)
            {
                message.Respond("Lobby not found", ResponseStatus.Failed);
                return;
            }

            var member = lobby.GetMemberByPeerId(peerId);

            if (member == null)
            {
                message.Respond("Player is not in the lobby", ResponseStatus.Failed);
                return;
            }

            message.Respond(member.GenerateDataPacket(), ResponseStatus.Success);
        }

        protected virtual void HandleGetLobbyInfo(IIncommingMessage message)
        {
            var lobbyId = message.AsInt();

            Lobbies.TryGetValue(lobbyId, out var lobby);

            if (lobby == null)
            {
                message.Respond("Lobby not found", ResponseStatus.Failed);
                return;
            }

            message.Respond(lobby.GenerateLobbyData(), ResponseStatus.Success);
        }

        #endregion

        public IEnumerable<GameInfoPacket> GetPublicGames(IPeer peer, Dictionary<string, string> filters)
        {
            return Lobbies.Values.Select(lobby => new GameInfoPacket()
            {
                Address = lobby.GameIp + ":" + lobby.GamePort,
                Id = lobby.Id,
                IsPasswordProtected = false,
                MaxPlayers = lobby.MaxPlayers,
                Name = lobby.Name,
                OnlinePlayers = lobby.PlayerCount,
                Properties = GetPublicLobbyProperties(peer, lobby, filters),
                Type = GameInfoType.Lobby
            });
        }

        public virtual Dictionary<string, string> GetPublicLobbyProperties(IPeer peer, ILobby lobby,
            Dictionary<string, string> playerFilters)
        {
            return lobby.GetPublicProperties(peer);
        }

    }
}
