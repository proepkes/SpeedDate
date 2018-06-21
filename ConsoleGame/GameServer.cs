using System;
using System.IO;
using System.Reflection;
using SpeedDate;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.GameServer;
using SpeedDate.Configuration;

namespace ConsoleGame
{
    class GameServer
    {
        private readonly SpeedDateClient client;

        public event Action ConnectedToMaster;

        public LobbiesPlugin Lobbies { get; private set; }
        public PeerInfoPlugin PeerInfo { get; private set; }
        public ProfilesPlugin Profiles { get; private set; }
        public RoomsPlugin Rooms { get; private set; }

        public GameServer()
        {
            client = new SpeedDateClient();
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start(string configFile)
        {
            client.Started += () => ConnectedToMaster?.Invoke();
            client.Start(new FileConfigProvider($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\{configFile}"));
            Lobbies = client.GetPlugin<LobbiesPlugin>();
            PeerInfo = client.GetPlugin<PeerInfoPlugin>();
            Profiles = client.GetPlugin<ProfilesPlugin>();
            Rooms = client.GetPlugin<RoomsPlugin>();
        }

        public void Stop()
        {
            client.Stop();
        }
    }
}
