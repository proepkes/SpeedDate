using System;
using System.IO;
using System.Reflection;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.GameServer;
using SpeedDate.Configuration;

namespace ConsoleGameServer.Example
{
    class GameServer
    {
        private readonly SpeedDateClient _client;

        public event Action ConnectedToMaster;

        public LobbiesPlugin Lobbies { get; private set; }
        public PeerInfoPlugin PeerInfo { get; private set; }
        public ProfilesPlugin Profiles { get; private set; }
        public RoomsPlugin Rooms { get; private set; }

        public GameServer()
        {
            _client = new SpeedDateClient();
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start(string configFile)
        {
            _client.Started += () => ConnectedToMaster?.Invoke();
            _client.Start(new FileConfigProvider($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\{configFile}"));
            Lobbies = _client.GetPlugin<LobbiesPlugin>();
            PeerInfo = _client.GetPlugin<PeerInfoPlugin>();
            Profiles = _client.GetPlugin<ProfilesPlugin>();
            Rooms = _client.GetPlugin<RoomsPlugin>();
        }

        public void Stop()
        {
            _client.Stop();
        }
    }
}
