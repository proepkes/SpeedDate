using System;
using System.IO;
using System.Reflection;
using SpeedDate.ClientPlugins.GameServer;

namespace ConsoleGame
{
    class GameServer
    {
        private readonly SpeedDate.SpeedDate _speedDate;

        public event Action ConnectedToMaster;

        public LobbiesPlugin Lobbies { get; private set; }
        public PeerInfoPlugin PeerInfo { get; private set; }
        public ProfilesPlugin Profiles { get; private set; }
        public RoomsPlugin Rooms { get; private set; }

        public GameServer(string configFile)
        {
            _speedDate = new SpeedDate.SpeedDate($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\{configFile}");
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start()
        {
            _speedDate.Started += () => ConnectedToMaster?.Invoke();
            _speedDate.Start();
            Lobbies = _speedDate.PluginProver.Get<LobbiesPlugin>();
            PeerInfo = _speedDate.PluginProver.Get<PeerInfoPlugin>();
            Profiles = _speedDate.PluginProver.Get<ProfilesPlugin>();
            Rooms = _speedDate.PluginProver.Get<RoomsPlugin>();
        }
    }
}
