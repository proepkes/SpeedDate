using System;
using SpeedDate.ClientPlugins.Spawner;

namespace SpeedDate.Client.Spawner.Console
{
    class Spawner
    {
        private readonly SpeedDater _speedDater;
        public event Action Connected;

        public SpawnerPlugin SpawnApi { get; private set; }

        public Spawner(string configFile)
        {
            _speedDater = new SpeedDater(configFile);
            _speedDater.Started += () => Connected?.Invoke();
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start()
        {
            _speedDater.Start();
            SpawnApi = _speedDater.PluginProver.Get<SpawnerPlugin>();
        }
    }
}
