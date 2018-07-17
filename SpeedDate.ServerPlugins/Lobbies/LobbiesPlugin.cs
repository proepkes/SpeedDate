using System;
using System.Collections.Generic;
using System.Linq;
using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Common;
using SpeedDate.Packets.Lobbies;
using SpeedDate.Packets.Matchmaking;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Matchmaker;
using SpeedDate.ServerPlugins.Rooms;
using SpeedDate.ServerPlugins.Spawner;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public sealed class LobbiesPlugin : SpeedDateServerPlugin, IGamesProvider
    {
        private const int CreateLobbiesPermissionLevel = 0;

        private readonly bool _dontAllowCreatingIfJoined = true;

        private readonly Dictionary<int, Lobby> _lobbies = new Dictionary<int, Lobby>();
        private readonly Dictionary<int, Func<LobbiesPlugin, Dictionary<string, string>, IPeer, Lobby>> _factories = new Dictionary<int, Func<LobbiesPlugin, Dictionary<string, string>, IPeer, Lobby>>();

        [Inject] private readonly ILogger _logger;
        [Inject] internal readonly RoomsPlugin RoomsPlugin;
        [Inject] internal readonly SpawnerPlugin SpawnerPlugin;

        private int _nextLobbyId;
        public int JoinedLobbiesLimit = 1;

        public IEnumerable<GameInfoPacket> GetPublicGames(IPeer peer, Dictionary<string, string> filters)
        {
            return _lobbies.Values.Select(lobby => new GameInfoPacket
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


        public override void Loaded()
        {
            Server.SetHandler(OpCodes.CreateLobby, HandleCreateLobby);
            Server.SetHandler(OpCodes.JoinLobby, HandleJoinLobby);
            Server.SetHandler(OpCodes.LeaveLobby, HandleLeaveLobby);
            Server.SetHandler(OpCodes.SetLobbyProperties, HandleSetLobbyProperties);
            Server.SetHandler(OpCodes.SetMyLobbyProperties, HandleSetMyProperties);
            Server.SetHandler(OpCodes.JoinLobbyTeam, HandleJoinTeam);
            Server.SetHandler(OpCodes.LobbySendChatMessage, HandleSendChatMessage);
            Server.SetHandler(OpCodes.LobbySetReady, HandleSetReadyStatus);
            Server.SetHandler(OpCodes.LobbyStartGame, HandleStartGame);
            Server.SetHandler(OpCodes.GetLobbyRoomAccess, HandleGetLobbyRoomAccess);

            Server.SetHandler(OpCodes.GetLobbyMemberData, HandleGetLobbyMemberData);
            Server.SetHandler(OpCodes.GetLobbyInfo, HandleGetLobbyInfo);
        }


        public void AddFactory(Func<LobbiesPlugin, Dictionary<string, string>, IPeer, Lobby> factory)
        {
            _factories[_factories.Keys.Max() + 1] = factory;
        }

        public bool AddLobby(Lobby lobby)
        {
            if (_lobbies.ContainsKey(lobby.Id))
            {
                _logger.Error("Failed to add a lobby - lobby with same id already exists");
                return false;
            }

            _lobbies.Add(lobby.Id, lobby);

            lobby.Destroyed += OnLobbyDestroyed;
            return true;
        }

        /// <summary>
        ///     Invoked, when lobby is destroyed
        /// </summary>
        /// <param name="lobby"></param>
        private void OnLobbyDestroyed(Lobby lobby)
        {
            _lobbies.Remove(lobby.Id);
            lobby.Destroyed -= OnLobbyDestroyed;
        }

        private LobbyUserExtension GetOrCreateLobbiesExtension(IPeer peer)
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

        private void HandleCreateLobby(IIncommingMessage message)
        {
            if (message.Peer.HasExtension<PeerSecurityExtension>() == false || message.Peer.GetExtension<PeerSecurityExtension>().PermissionLevel < CreateLobbiesPermissionLevel)
            {
                message.Respond("Insufficient permissions", ResponseStatus.Unauthorized);
                return;
            }

            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);

            if (_dontAllowCreatingIfJoined && lobbiesExt.CurrentLobby != null)
            {
                // If peer is already in a lobby
                message.Respond("You are already in a lobby", ResponseStatus.Failed);
                return;
            }

            // Deserialize properties of the lobby
            var properties = new Dictionary<string, string>().FromBytes(message.AsBytes());

            if (!properties.ContainsKey(OptionKeys.LobbyFactoryId) || !int.TryParse(properties[OptionKeys.LobbyFactoryId], out var lobbyTypeId))
            {
                message.Respond("Invalid request (undefined factory)", ResponseStatus.Failed);
                return;
            }

            // Get the lobby factory
            _factories.TryGetValue(lobbyTypeId, out var factory);

            if (factory == null)
            {
                message.Respond("Unavailable lobby type", ResponseStatus.Failed);
                return;
            }

            var newLobby = factory.Invoke(this, properties, message.Peer);

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
        ///     Handles a request from user to join a lobby
        /// </summary>
        /// <param name="message"></param>
        private void HandleJoinLobby(IIncommingMessage message)
        {
            var user = GetOrCreateLobbiesExtension(message.Peer);

            if (user.CurrentLobby != null)
            {
                message.Respond("You're already in a lobby", ResponseStatus.Failed);
                return;
            }

            var lobbyId = message.AsInt();

            _lobbies.TryGetValue(lobbyId, out var lobby);

            if (lobby == null)
            {
                message.Respond("Lobby was not found", ResponseStatus.Failed);
                return;
            }

            if (!lobby.AddPlayer(user,
                error => message.Respond(error ?? "Failed to add player to lobby", ResponseStatus.Failed)))
                return;

            var data = lobby.GenerateLobbyData(user);

            message.Respond(data, ResponseStatus.Success);
        }

        /// <summary>
        ///     Handles a request from user to leave a lobby
        /// </summary>
        /// <param name="message"></param>
        private void HandleLeaveLobby(IIncommingMessage message)
        {
            var lobbyId = message.AsInt();

            _lobbies.TryGetValue(lobbyId, out var lobby);

            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);

            lobby?.RemovePlayer(lobbiesExt);

            message.Respond(ResponseStatus.Success);
        }

        private void HandleSetLobbyProperties(IIncommingMessage message)
        {
            var data = message.Deserialize<LobbyPropertiesSetPacket>();

            _lobbies.TryGetValue(data.LobbyId, out var lobby);

            if (lobby == null)
            {
                message.Respond("Lobby was not found", ResponseStatus.Failed);
                return;
            }

            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);

            foreach (var dataProperty in data.Properties)
                if (!lobby.SetProperty(lobbiesExt, dataProperty.Key, dataProperty.Value))
                {
                    message.Respond("Failed to set the property: " + dataProperty.Key,
                        ResponseStatus.Failed);
                    return;
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
                // We don't change properties directly,
                // because we want to allow an implementation of lobby
                // to do "sanity" checking
                if (!lobby.SetPlayerProperty(player, dataProperty.Key, dataProperty.Value))
                {
                    message.Respond("Failed to set property: " + dataProperty.Key, ResponseStatus.Failed);
                    return;
                }

            message.Respond(ResponseStatus.Success);
        }

        private void HandleSetReadyStatus(IIncommingMessage message)
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

        private void HandleJoinTeam(IIncommingMessage message)
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

        private void HandleSendChatMessage(IIncommingMessage message)
        {
            var lobbiesExt = GetOrCreateLobbiesExtension(message.Peer);
            var lobby = lobbiesExt.CurrentLobby;

            var member = lobby.GetMember(lobbiesExt);

            // Invalid request
            if (member == null)
                return;

            lobby.HandleChatMessage(member, message);
        }

        private void HandleStartGame(IIncommingMessage message)
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

        private void HandleGetLobbyRoomAccess(IIncommingMessage message)
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

        private void HandleGetLobbyMemberData(IIncommingMessage message)
        {
            var data = message.Deserialize<IntPairPacket>();
            var lobbyId = data.A;
            var peerId = data.B;

            _lobbies.TryGetValue(lobbyId, out var lobby);

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

        private void HandleGetLobbyInfo(IIncommingMessage message)
        {
            var lobbyId = message.AsInt();

            _lobbies.TryGetValue(lobbyId, out var lobby);

            if (lobby == null)
            {
                message.Respond("Lobby not found", ResponseStatus.Failed);
                return;
            }

            message.Respond(lobby.GenerateLobbyData(), ResponseStatus.Success);
        }

        public Dictionary<string, string> GetPublicLobbyProperties(IPeer peer, Lobby lobby,
            Dictionary<string, string> playerFilters)
        {
            return lobby.GetPublicProperties(peer);
        }
    }
}