using System;
using SpeedDate.ClientPlugins.Spawner;
using SpeedDate.Configuration;

namespace SpeedDate.Client.Spawner.Console
{
    class Spawner
    {
        private readonly SpeedDateClient client;
        public event Action Connected;

        public SpawnerPlugin SpawnApi { get; private set; }

        public Spawner()
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
            SpawnApi = client.GetPlugin<SpawnerPlugin>();
        }
    }
}
