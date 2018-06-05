using System;
using SpeedDate.ClientPlugins.Spawner;

namespace SpeedDate.Client.Console.Example
{
    class Spawner
    {
        private readonly SpeedDate _speedDate;
        public event Action Connected;

        public SpawnerPlugin Spawn { get; private set; }

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
            Spawn = _speedDate.PluginProver.Get<SpawnerPlugin>();
        }
    }
}
