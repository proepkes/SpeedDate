using System;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Chat;
using SpeedDate.ClientPlugins.Peer.Lobby;
using SpeedDate.ClientPlugins.Peer.MatchMaker;
using SpeedDate.ClientPlugins.Peer.Profile;
using SpeedDate.ClientPlugins.Peer.Room;
using SpeedDate.ClientPlugins.Peer.Security;
using SpeedDate.ClientPlugins.Peer.SpawnRequest;
using SpeedDate.Configuration;

namespace SpeedDate.Client.Console.Example
{
    class GameClient
    {
        private readonly SpeedDateClient client;

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
            client = new SpeedDateClient();
            client.Started += () => Connected?.Invoke();
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start(IConfigProvider configProvider)
        {
            client.Start(configProvider);
            Auth = client.GetPlugin<AuthPlugin>();
            Chat = client.GetPlugin<ChatPlugin>();
            Lobby = client.GetPlugin<LobbyPlugin>();
            Matchmaker = client.GetPlugin<MatchmakerPlugin>();
            Profile = client.GetPlugin<ProfilePlugin>();
            Room = client.GetPlugin<RoomPlugin>();
            Security = client.GetPlugin<SecurityPlugin>();
            Spawn = client.GetPlugin<SpawnRequestPlugin>();
        }
    }
}
