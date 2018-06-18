using System;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Chat;
using SpeedDate.ClientPlugins.Peer.Lobbies;
using SpeedDate.ClientPlugins.Peer.MatchMaker;
using SpeedDate.ClientPlugins.Peer.Profiles;
using SpeedDate.ClientPlugins.Peer.Rooms;
using SpeedDate.ClientPlugins.Peer.Security;
using SpeedDate.ClientPlugins.Peer.SpawnRequest;
using SpeedDate.Configuration;

namespace SpeedDate.Client.Console.Example
{
    class GameClient
    {
        private readonly SpeedDater _speedDater;

        public event Action Connected;

        public AuthPlugin Auth { get; private set; }
        public ChatPlugin Chat { get; private set; }
        public LobbyPlugin Lobby { get; private set; }
        public MatchmakerPlugin Matchmaker { get; private set; }
        public ProfilePlugin Profile { get; private set; }
        public RoomPlugin Room { get; private set; }
        public SecurityPlugin Security { get; private set; }
        public SpawnRequestPlugin Spawn { get; private set; }

        public GameClient()
        {
            _speedDater = new SpeedDater();
            _speedDater.Started += () => Connected?.Invoke();
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start(IConfigProvider configProvider)
        {
            _speedDater.Start(configProvider);
            Auth = _speedDater.PluginProver.Get<AuthPlugin>();
            Chat = _speedDater.PluginProver.Get<ChatPlugin>();
            Lobby = _speedDater.PluginProver.Get<LobbyPlugin>();
            Matchmaker = _speedDater.PluginProver.Get<MatchmakerPlugin>();
            Profile = _speedDater.PluginProver.Get<ProfilePlugin>();
            Room = _speedDater.PluginProver.Get<RoomPlugin>();
            Security = _speedDater.PluginProver.Get<SecurityPlugin>();
            Spawn = _speedDater.PluginProver.Get<SpawnRequestPlugin>();
        }
    }
}
