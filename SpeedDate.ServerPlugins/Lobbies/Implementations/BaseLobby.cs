using System;
using System.Collections.Generic;
using System.Linq;

using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Network.LiteNetLib;
using SpeedDate.Packets.Common;
using SpeedDate.Packets.Lobbies;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Rooms;
using SpeedDate.ServerPlugins.Spawner;
using SpawnStatus = SpeedDate.Packets.Spawner.SpawnStatus;

namespace SpeedDate.ServerPlugins.Lobbies.Implementations
{
    class BaseLobby : ILobby
    {
        private Packets.Lobbies.LobbyState _state;
        private string _statusText = "";
        private LobbyMember _gameMaster;

        public event Action<LobbyMember> PlayerAdded;
        public event Action<LobbyMember> PlayerRemoved;

        public event Action<ILobby> Destroyed;

        public Logger Logger = LogManager.GetLogger(typeof(BaseLobby).Name);

        protected Dictionary<string, LobbyMember> Members;
        protected Dictionary<long, LobbyMember> MembersByPeerId;
        protected Dictionary<string, string> Properties;
        protected Dictionary<string, LobbyTeam> Teams;
        protected HashSet<IPeer> Subscribers;

        protected List<LobbyPropertyData> Controls;

        protected SpawnTask GameSpawnTask;
        protected RegisteredRoom Room;

        public BaseLobby(int lobbyId, IEnumerable<LobbyTeam> teams,
            LobbiesPlugin plugin, LobbyConfig config)
        {
            Id = lobbyId;
            Plugin = plugin;
            GameIp = "";
            GamePort = -1;

            Config = config;

            Controls = new List<LobbyPropertyData>();
            Members = new Dictionary<string, LobbyMember>();
            MembersByPeerId = new Dictionary<long, LobbyMember>();
            Properties = new Dictionary<string, string>();
            Teams = teams.ToDictionary(t => t.Name, t => t);
            Subscribers = new HashSet<IPeer>();

            MaxPlayers = Teams.Values.Sum(t => t.MaxPlayers);
            MinPlayers = Teams.Values.Sum(t => t.MinPlayers);
        }

        public string Name { get; set; }
        public int PlayerCount { get { return Members.Count; } }

        public int Id { get; private set; }
        protected LobbiesPlugin Plugin { get; private set; }

        public bool IsDestroyed { get; private set; }

        public LobbyConfig Config { get; private set; }

        public int MaxPlayers { get; protected set; }
        public int MinPlayers { get; protected set; }

        public string Type { get; set; }
        public string GameIp { get; protected set; }
        public int GamePort { get; protected set; }

        public Packets.Lobbies.LobbyState State
        {
            get => _state;
            protected set
            {
                if (_state == value)
                    return;

                _state = value;
                OnLobbyStateChange(value);
            }
        }

        public string StatusText
        {
            get => _statusText;
            protected set
            {
                if (_statusText == value)
                    return;

                OnStatusTextChange(value);
            }
        }

        protected LobbyMember GameMaster
        {
            get => _gameMaster;
            set
            {
                if (!Config.EnableGameMasters)
                    return;
                _gameMaster = value;
                OnGameMasterChange();
            }
        }

        public bool AddPlayer(LobbyUserExtension playerExt, out string error)
        {
            error = null;

            if (playerExt.CurrentLobby != null)
            {
                error = "You're already in a lobby";
                return false;
            }

            var username = TryGetUsername(playerExt.Peer);

            if (username == null)
            {
                error = "Invalid username";
                return false;
            }

            if (Members.ContainsKey(username))
            {
                error = "Already in the lobby";
                return false;
            }

            if (IsDestroyed)
            {
                error = "Lobby is destroyed";
                return false;
            }

            if (!IsPlayerAllowed(username, playerExt))
            {
                error = "You're not allowed";
                return false;
            }

            if (Members.Values.Count >= MaxPlayers)
            {
                error = "Lobby is full";
                return false;
            }

            if (!Config.AllowJoiningWhenGameIsLive && State != Packets.Lobbies.LobbyState.Preparations)
            {
                error =  "Game is already in progress";
                return false;
            }

            // Create an "instance" of the member
            var member = CreateMember(username, playerExt);

            // Add it to a team
            var team = PickTeamForPlayer(member);

            if (team == null)
            {
                error = "Invalid lobby team";
                return false;
            }

            if (!team.AddMember(member))
            {
                error = "Not allowed to join a team";
                return false;
            }

            Members[member.Username] = member;
            MembersByPeerId[playerExt.Peer.ConnectId] = member;

            // Set this lobby as player's current lobby
            playerExt.CurrentLobby = this;

            if (GameMaster == null)
                PickNewGameMaster(false);

            Subscribe(playerExt.Peer);

            playerExt.Peer.Disconnected += OnPeerDisconnected;

            OnPlayerAdded(member);

            PlayerAdded?.Invoke(member);


            return true;
        }

        public void RemovePlayer(LobbyUserExtension playerExt)
        {
            var username = TryGetUsername(playerExt.Peer);

            Members.TryGetValue(username, out var member);

            // If this player was never in the lobby
            if (member == null)
                return;

            Members.Remove(username);
            MembersByPeerId.Remove(playerExt.Peer.ConnectId);

            if (playerExt.CurrentLobby == this)
                playerExt.CurrentLobby = null;

            // Remove member from it's current team
            member.Team?.RemoveMember(member);

            // Change the game master
            if (GameMaster == member)
                PickNewGameMaster();


            // Unsubscribe
            playerExt.Peer.Disconnected -= OnPeerDisconnected;
            Unsubscribe(playerExt.Peer);

            // Notify player himself that he's removed
            playerExt.Peer.SendMessage((ushort) OpCodes.LeftLobby, Id);

            OnPlayerRemoved(member);

            PlayerRemoved?.Invoke(member);
        }

        public virtual bool SetProperty(LobbyUserExtension setter, string key, string value)
        {
            if (!Config.AllowPlayersChangeLobbyProperties)
                return false;

            if (Config.EnableGameMasters)
            {
                MembersByPeerId.TryGetValue(setter.Peer.ConnectId, out var member);

                if (GameMaster != member)
                    return false;
            }

            return SetProperty(key, value);
        }

        public bool SetProperty(string key, string value)
        {
            if (Properties.ContainsKey(key))
            {
                Properties[key] = value;
            }
            else
            {
                Properties.Add(key, value);
            }

            OnLobbyPropertyChange(key);
            return true;
        }

        public LobbyMember GetMember(LobbyUserExtension playerExt)
        {
            MembersByPeerId.TryGetValue(playerExt.Peer.ConnectId, out var member);

            return member;
        }

        public LobbyMember GetMember(string username)
        {
            Members.TryGetValue(username, out var member);

            return member;
        }

        public LobbyMember GetMemberByPeerId(int peerId)
        {
            MembersByPeerId.TryGetValue(peerId, out var member);

            return member;
        }

        public bool SetPlayerProperty(LobbyMember player, string key, string value)
        {
            // Invalid property
            if (key == null)
                return false;

            // Check if player is allowed to change this property
            if (!IsPlayerPropertyChangeable(player, key, value))
                return false;

            player.SetProperty(key, value);

            OnPlayerPropertyChange(player, key);

            return true;
        }

        public void SetLobbyProperties(Dictionary<string, string> properties)
        {
            foreach (var property in properties)
            {
                Properties[property.Key] = property.Value;
            }
        }

        public void SetReadyState(LobbyMember member, bool state)
        {
            if (!Members.ContainsKey(member.Username))
                return;

            member.IsReady = state;

            OnPlayerReadyStatusChange(member);

            if (Members.Values.All(m => m.IsReady))
                OnAllPlayersReady();
        }

        public void AddControl(LobbyPropertyData propertyData, string defaultValue)
        {
            SetProperty(propertyData.PropertyKey, defaultValue);
            Controls.Add(propertyData);
        }

        public void AddControl(LobbyPropertyData propertyData)
        {
            var defaultValue = "";

            if (propertyData.Options != null && propertyData.Options.Count > 0)
            {
                defaultValue = propertyData.Options.First();
            }

            SetProperty(propertyData.PropertyKey, defaultValue);
            Controls.Add(propertyData);
        }

        public bool TryJoinTeam(string teamName, LobbyMember member)
        {
            if (!Config.EnableTeamSwitching)
                return false;

            var currentTeam = member.Team;
            var newTeam = Teams[teamName];

            // Ignore, if any of the teams is invalid
            if (currentTeam == null || newTeam == null)
                return false;

            if (newTeam.PlayerCount >= newTeam.MaxPlayers)
            {
                SendChatMessage(member, "Team is full", true);
                return false;
            }

            // Try to add the member
            if (!newTeam.AddMember(member))
                return false;

            // Remove member from previous team
            currentTeam.RemoveMember(member);

            OnPlayerTeamChanged(member, newTeam);

            return true;
        }

        protected virtual LobbyMember CreateMember(string username, LobbyUserExtension extension)
        {
            return new LobbyMember(username, extension);
        }

        protected virtual void PickNewGameMaster(bool broadcastChange = true)
        {
            if (!Config.EnableGameMasters)
                return;

            GameMaster = Members.Values.FirstOrDefault();
        }

        public virtual LobbyTeam PickTeamForPlayer(LobbyMember member)
        {
            return Teams.Values
                .Where(t => t.CanAddPlayer(member))
                .OrderBy(t => t.PlayerCount).FirstOrDefault();
        }

        /// <summary>
        /// Extracts username of the peer.
        /// By default, uses user extension <see cref="IUserExtension"/>
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        protected virtual string TryGetUsername(IPeer peer)
        {
            var userExt = peer.GetExtension<IUserExtension>();

            return userExt?.Username;
        }

        /// <summary>
        /// This will be called before adding a player to lobby.
        /// Override it to add custom checks for bans and etc.
        /// </summary>
        protected virtual bool IsPlayerAllowed(string username, LobbyUserExtension user)
        {
            return true;
        }

        protected virtual bool IsPlayerPropertyChangeable(LobbyMember member, string key, string value)
        {
            return true;
        }

        public void Subscribe(IPeer peer)
        {
            Subscribers.Add(peer);
        }

        public void Unsubscribe(IPeer peer)
        {
            Subscribers.Remove(peer);
        }

        public virtual bool StartGame()
        {
            if (IsDestroyed)
                return false;

            var region = "";

            Properties[OptionKeys.IsPublic] = "false";

            // Extract the region if available
            if (Properties.ContainsKey(OptionKeys.Region))
                region = Properties[OptionKeys.Region];

            var task = Plugin.SpawnerPlugin.Spawn(Properties, region, GenerateCmdArgs());

            if (task == null)
            {
                BroadcastChatMessage("Servers are busy", true);
                return false;
            }

            State = Packets.Lobbies.LobbyState.StartingGameServer;

            SetGameSpawnTask(task);

            return true;
        }

        public void Destroy()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;

            // Remove players
            foreach (var member in Members.Values.ToList())
            {
                RemovePlayer(member.Extension);
            }

            if (GameSpawnTask != null)
            {
                GameSpawnTask.StatusChanged -= OnSpawnServerStatusChanged;
                GameSpawnTask.KillSpawnedProcess();
            }

            Destroyed?.Invoke(this);
        }

        protected virtual string GenerateCmdArgs()
        {
            return CommandLineArgs.Names.LobbyId + " " + Id;
        }

        public void SetGameSpawnTask(SpawnTask task)
        {
            if (task == null)
                return;

            if (GameSpawnTask == task)
                return;

            if (GameSpawnTask != null)
            {
                // Unsubscribe from previous game
                GameSpawnTask.StatusChanged -= OnSpawnServerStatusChanged;
                GameSpawnTask.Abort();
            }

            GameSpawnTask = task;

            task.StatusChanged += OnSpawnServerStatusChanged;
        }

        protected virtual void OnSpawnServerStatusChanged(SpawnStatus status)
        {
            var isStarting = status > SpawnStatus.None && status < SpawnStatus.Finalized;

            // If the game is currently starting
            if (isStarting && State != Packets.Lobbies.LobbyState.StartingGameServer)
            {
                State = Packets.Lobbies.LobbyState.StartingGameServer;
                return;
            }

            // If game is running
            if (status == SpawnStatus.Finalized)
            {
                State = Packets.Lobbies.LobbyState.GameInProgress;
                OnGameServerFinalized();
            }

            // If game is aborted / closed
            if (status < SpawnStatus.None)
            {
                // If game was open before
                if (State == Packets.Lobbies.LobbyState.StartingGameServer)
                {
                    State = Config.PlayAgainEnabled ? Packets.Lobbies.LobbyState.Preparations : Packets.Lobbies.LobbyState.FailedToStart;
                    BroadcastChatMessage("Failed to start a game server", true);
                }
                else
                {
                    State = Config.PlayAgainEnabled ? Packets.Lobbies.LobbyState.Preparations : Packets.Lobbies.LobbyState.GameOver;
                }
            }
        }

        protected virtual void OnGameServerFinalized()
        {
            if (GameSpawnTask.FinalizationPacket == null)
                return;

            var data = GameSpawnTask.FinalizationPacket.FinalizationData;

            if (!data.ContainsKey(OptionKeys.RoomId))
            {
                BroadcastChatMessage("Game server finalized, but room ID cannot be found", true);
                return;
            }

            // Get room id from finalization data
            var roomId = int.Parse(data[OptionKeys.RoomId]);
            var room = Plugin.RoomsPlugin.GetRoom(roomId);

            if (room == null)
                return;

            Room = room;

            GameIp = room.Options.RoomIp;
            GamePort = room.Options.RoomPort;

            room.Destroyed += OnRoomDestroyed;
        }

        public void OnRoomDestroyed(RegisteredRoom room)
        {
            room.Destroyed -= OnRoomDestroyed;

            GameIp = "";
            GamePort = -1;
            Room = null;

            GameSpawnTask = null;

            State = Config.PlayAgainEnabled ? Packets.Lobbies.LobbyState.Preparations : Packets.Lobbies.LobbyState.GameOver;
        }

        public Dictionary<string, string> GetPublicProperties(IPeer peer)
        {
            return Properties;
        }

        #region Packet generators

        public LobbyDataPacket GenerateLobbyData()
        {
            var info = new LobbyDataPacket
            {
                LobbyType = Type ?? "",
                GameMaster = GameMaster != null ? GameMaster.Username : "",
                LobbyName = Name,
                LobbyId = Id,
                LobbyProperties = Properties,
                Players = Members.Values
                    .ToDictionary(m => m.Username, GenerateMemberData),
                Teams = Teams.Values.ToDictionary(t => t.Name, t => t.GenerateData()),
                Controls = Controls,
                LobbyState = State,
                MaxPlayers = MaxPlayers,
                EnableTeamSwitching = Config.EnableTeamSwitching,
                EnableReadySystem = Config.EnableReadySystem,
                EnableManualStart = Config.EnableManualStart,
                CurrentUserUsername = ""
            };

            return info;
        }

        public LobbyDataPacket GenerateLobbyData(LobbyUserExtension user)
        {
            var info = new LobbyDataPacket
            {
                LobbyType = Type ?? "",
                GameMaster = GameMaster != null ? GameMaster.Username : "",
                LobbyName = Name,
                LobbyId = Id,
                LobbyProperties = Properties,
                Players = Members.Values
                    .ToDictionary(m => m.Username, GenerateMemberData),
                Teams = Teams.Values.ToDictionary(t => t.Name, t => t.GenerateData()),
                Controls = Controls,
                LobbyState = State,
                MaxPlayers = MaxPlayers,
                EnableTeamSwitching = Config.EnableTeamSwitching,
                EnableReadySystem = Config.EnableReadySystem,
                EnableManualStart = Config.EnableManualStart,
                CurrentUserUsername = TryGetUsername(user.Peer)
            };

            return info;
        }

        public void HandleChatMessage(LobbyMember member, IIncommingMessage message)
        {
            var text = message.AsString();

            var messagePacket = new LobbyChatPacket()
            {
                Message = text,
                Sender = member.Username
            };

            var msg = MessageHelper.Create((ushort)OpCodes.LobbyChatMessage, messagePacket.ToBytes());

            Broadcast(msg);
        }

        public void HandleGameAccessRequest(IIncommingMessage message)
        {
            if (Room == null)
            {
                message.Respond("Game is not running", ResponseStatus.Failed);
                return;
            }

            var requestData = new Dictionary<string, string>().FromBytes(message.AsBytes());

            Room.GetAccess(message.Peer, requestData, (access) =>
            {
                // Send back the access
                message.Respond(access, ResponseStatus.Success);
            }, error =>
            {
                message.Respond($"Failed to get access to game: {error}", ResponseStatus.Failed);
            });
        }

        public virtual bool StartGameManually(LobbyUserExtension user)
        {
            var member = GetMember(user);

            if (!Config.EnableManualStart)
            {
                SendChatMessage(member, "You cannot start the game manually", true);
                return false;
            }

            // If not game maester
            if (GameMaster != member)
            {
                SendChatMessage(member, "You're not the master of this game", true);
                return false;
            }

            if (State != Packets.Lobbies.LobbyState.Preparations)
            {
                SendChatMessage(member, "Invalid lobby state", true);
                return false;
            }

            if (IsDestroyed)
            {
                SendChatMessage(member, "Lobby is destroyed", true);
                return false;
            }

            if (Members.Values.Any(m => !m.IsReady && m != _gameMaster))
            {
                SendChatMessage(member, "Not all players are ready", true);
                return false;
            }

            if (Members.Count < MinPlayers)
            {
                SendChatMessage(
                    member,
                    $"Not enough players. Need {MinPlayers - Members.Count} more ",
                    true);
                return false;
            }

            var lackingTeam = Teams.Values.FirstOrDefault(t => t.MinPlayers > t.PlayerCount);

            if (lackingTeam != null)
            {
                var msg = $"Team {lackingTeam.Name} does not have enough players";
                SendChatMessage(member, msg, true);
                return false;
            }

            return StartGame();
        }

        public virtual LobbyMemberData GenerateMemberData(LobbyMember member)
        {
            return member.GenerateDataPacket();
        }

        #endregion

        #region Broadcasting

        public void Broadcast(IMessage message)
        {
            foreach (var peer in Subscribers)
            {
                peer.SendMessage(message, DeliveryMethod.ReliableUnordered);
            }
        }

        public void Broadcast(IMessage message, Func<IPeer, bool> condition)
        {
            foreach (var peer in Subscribers)
            {
                if (!condition(peer))
                    continue;

                peer.SendMessage(message, DeliveryMethod.ReliableUnordered);
            }
        }

        public void BroadcastChatMessage(string message, bool isError = false,
            string sender = "System")
        {
            var msg = new LobbyChatPacket()
            {
                Message = message,
                Sender = sender,
                IsError = isError
            };

            Broadcast(MessageHelper.Create((ushort)OpCodes.LobbyChatMessage, msg.ToBytes()));
        }

        public void SendChatMessage(LobbyMember member, string message, bool isError = false,
            string sender = "System")
        {
            var packet = new LobbyChatPacket()
            {
                Message = message,
                Sender = sender,
                IsError = isError
            };

            var msg = MessageHelper.Create((ushort)OpCodes.LobbyChatMessage, packet.ToBytes());

            member.Extension.Peer.SendMessage(msg, DeliveryMethod.ReliableUnordered);
        }

        #endregion

        #region On... Stuff

        protected virtual void OnPlayerAdded(LobbyMember member)
        {
            // Notify others about the new user
            var msg = MessageHelper.Create((ushort)OpCodes.LobbyMemberJoined, member.GenerateDataPacket().ToBytes());

            // Don't send to the person who just joined
            Broadcast(msg, p => p != member.Extension.Peer);
        }

        protected virtual void OnPlayerRemoved(LobbyMember member)
        {
            // Destroy lobby if last member left
            if (!Config.KeepAliveWithZeroPlayers && Members.Count == 0)
            {
                Destroy();
                Logger.Log(LogLevel.Info, $"Lobby \"{Name}\" destroyed due to last player leaving.");
            }

            // Notify others about the user who left
            Broadcast(MessageHelper.Create((ushort)OpCodes.LobbyMemberLeft, member.Username));
        }

        protected virtual void OnLobbyStateChange(Packets.Lobbies.LobbyState state)
        {
            switch (state)
            {
                case Packets.Lobbies.LobbyState.FailedToStart:
                    StatusText = "Failed to start server";
                    break;
                case Packets.Lobbies.LobbyState.Preparations:
                    StatusText = "Failed to start server";
                    break;
                case Packets.Lobbies.LobbyState.StartingGameServer:
                    StatusText = "Starting game server";
                    break;
                case Packets.Lobbies.LobbyState.GameInProgress:
                    StatusText = "Game in progress";
                    break;
                case Packets.Lobbies.LobbyState.GameOver:
                    StatusText = "Game is over";
                    break;
                default:
                    StatusText = "Unknown lobby state";
                    break;
            }

            // Disable ready states
            foreach (var lobbyMember in Members.Values)
                SetReadyState(lobbyMember, false);

            var msg = MessageHelper.Create((ushort) OpCodes.LobbyStateChange, (int)state);
            Broadcast(msg);
        }

        private void OnStatusTextChange(string text)
        {
            var msg = MessageHelper.Create((ushort)OpCodes.LobbyStatusTextChange, text);
            Broadcast(msg);
        }

        protected virtual void OnLobbyPropertyChange(string propertyKey)
        {
            var packet = new StringPairPacket()
            {
                A = propertyKey,
                B = Properties[propertyKey]
            };

            // Broadcast new properties
            Broadcast(MessageHelper.Create((ushort)OpCodes.LobbyPropertyChanged, packet.ToBytes()));
        }

        protected virtual void OnPlayerPropertyChange(LobbyMember member, string propertyKey)
        {
            // Broadcast the changes
            var changesPacket = new LobbyMemberPropChangePacket()
            {
                LobbyId = Id,
                Username = member.Username,
                Property = propertyKey,
                Value = member.GetProperty(propertyKey)
            };

            Broadcast(MessageHelper.Create((ushort)OpCodes.LobbyMemberPropertyChanged, changesPacket.ToBytes()));
        }

        protected virtual void OnPlayerTeamChanged(LobbyMember member, LobbyTeam newTeam)
        {
            var packet = new StringPairPacket()
            {
                A = member.Username,
                B = newTeam.Name
            };

            // Broadcast the change
            var msg = MessageHelper.Create((ushort)OpCodes.LobbyMemberChangedTeam, packet.ToBytes());
            Broadcast(msg);
        }

        /// <summary>
        /// Invoked when one of the members disconnects
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnPeerDisconnected(IPeer peer)
        {
            RemovePlayer(peer.GetExtension<LobbyUserExtension>());
        }

        protected virtual void OnPlayerReadyStatusChange(LobbyMember member)
        {
            // Broadcast the new status
            var packet = new StringPairPacket()
            {
                A = member.Username,
                B = member.IsReady.ToString()
            };

            Broadcast(MessageHelper.Create((ushort)OpCodes.LobbyMemberReadyStatusChange, packet.ToBytes()));
        }

        protected virtual void OnGameMasterChange()
        {
            var masterUsername = GameMaster != null ? GameMaster.Username : "";
            var msg = MessageHelper.Create((ushort)OpCodes.LobbyMasterChange, masterUsername);
            Broadcast(msg);
        }

        protected virtual void OnAllPlayersReady()
        {
            if (!Config.StartGameWhenAllReady)
                return;

            if (Teams.Values.Any(t => t.PlayerCount < t.MinPlayers))
                return;

            StartGame();
        }

        #endregion
    }
}
