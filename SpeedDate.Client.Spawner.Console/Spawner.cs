using System;
using SpeedDate.ClientPlugins.Spawner;
using SpeedDate.Configuration;

namespace SpeedDate.Client.Spawner.Console
{
    class Spawner
    {
        private readonly SpeedDater _speedDater;
        public event Action Connected;

        public SpawnerPlugin SpawnApi { get; private set; }

        public Spawner()
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
            SpawnApi = _speedDater.PluginProver.Get<SpawnerPlugin>();
        }
    }
}
