﻿using System;
using SpeedDate;

namespace ConsoleGame
{
    class GameServer
    {
        private readonly SpeedDater _speedDater;

        public event Action ConnectedToMaster;

        public LobbiesPlugin Lobbies { get; private set; }
        public PeerInfoPlugin PeerInfo { get; private set; }
        public ProfilesPlugin Profiles { get; private set; }
        public RoomsPlugin Rooms { get; private set; }

        public GameServer(string configFile)
        {
            _speedDater = new SpeedDater($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\{configFile}");
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start()
        {
            _speedDater.Started += () => ConnectedToMaster?.Invoke();
            _speedDater.Start();
            Lobbies = _speedDater.PluginProver.Get<LobbiesPlugin>();
            PeerInfo = _speedDater.PluginProver.Get<PeerInfoPlugin>();
            Profiles = _speedDater.PluginProver.Get<ProfilesPlugin>();
            Rooms = _speedDater.PluginProver.Get<RoomsPlugin>();
        }

        public void Stop()
        {
            _speedDater.Stop();
        }
    }
}
