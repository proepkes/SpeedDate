using System;
using SpeedDate.ClientPlugins.Spawner;

namespace SpeedDate.Client.Spawner.Console
{
    class Spawner
    {
        private readonly SpeedDate _speedDate;
        public event Action Connected;

        public SpawnerPlugin SpawnApi { get; private set; }

        public Spawner(string configFile)
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
            SpawnApi = _speedDate.PluginProver.Get<SpawnerPlugin>();
        }
    }
}
