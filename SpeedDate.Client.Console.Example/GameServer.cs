using SpeedDate.ClientPlugins.GameServer;

namespace SpeedDate.Client.Console.Example
{
    class GameServer
    {
        private readonly SpeedDate _speedDate;

        public LobbiesPlugin Lobbies { get; private set; }
        public PeerInfoPlugin PeerInfo { get; private set; }
        public ProfilesPlugin Profiles { get; private set; }
        public RoomsPlugin Rooms { get; private set; }

        public GameServer(string configFile)
        {
            _speedDate = new SpeedDate(configFile);
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start()
        {
            _speedDate.Start();
            Lobbies = _speedDate.PluginProver.Get<LobbiesPlugin>();
            PeerInfo = _speedDate.PluginProver.Get<PeerInfoPlugin>();
            Profiles = _speedDate.PluginProver.Get<ProfilesPlugin>();
            Rooms = _speedDate.PluginProver.Get<RoomsPlugin>();
        }
    }
}
