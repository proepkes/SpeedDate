using System;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Chat;
using SpeedDate.ClientPlugins.Peer.Lobbies;
using SpeedDate.ClientPlugins.Peer.MatchMaker;
using SpeedDate.ClientPlugins.Peer.Profiles;
using SpeedDate.ClientPlugins.Peer.Rooms;
using SpeedDate.ClientPlugins.Peer.Security;
using SpeedDate.ClientPlugins.Peer.SpawnRequest;

namespace SpeedDate.Client.Console.Example
{
    class GameClient
    {
        private readonly SpeedDate _speedDate;

        public event Action Connected;

        public AuthPlugin Auth { get; private set; }
        public ChatPlugin Chat { get; private set; }
        public LobbyPlugin Lobby { get; private set; }
        public MatchmakerPlugin Matchmaker { get; private set; }
        public ProfilePlugin Profile { get; private set; }
        public RoomPlugin Room { get; private set; }
        public SecurityPlugin Security { get; private set; }
        public SpawnRequestPlugin Spawn { get; private set; }

        public GameClient(string configFile)
        {
            _speedDate = new SpeedDate(configFile);
            _speedDate.Started += () => Connected?.Invoke();
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start()
        {
            _speedDate.Start();
            Auth = _speedDate.PluginProver.Get<AuthPlugin>();
            Chat = _speedDate.PluginProver.Get<ChatPlugin>();
            Lobby = _speedDate.PluginProver.Get<LobbyPlugin>();
            Matchmaker = _speedDate.PluginProver.Get<MatchmakerPlugin>();
            Profile = _speedDate.PluginProver.Get<ProfilePlugin>();
            Room = _speedDate.PluginProver.Get<RoomPlugin>();
            Security = _speedDate.PluginProver.Get<SecurityPlugin>();
            Spawn = _speedDate.PluginProver.Get<SpawnRequestPlugin>();
        }
    }
}
