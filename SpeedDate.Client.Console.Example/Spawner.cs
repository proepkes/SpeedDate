using SpeedDate.ClientPlugins.Spawner;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.Client
{
    class Spawner
    {
        private readonly SpeedDate _speedDate;

        public SpawnerPlugin Spawn { get; private set; }

        public Spawner(string configFile)
        {
            _speedDate = new SpeedDate(configFile);
            _speedDate.Started += SpeedDateOnStarted;
        }

        /// <summary>
        /// Connects to Master and loads the Plugins
        /// </summary>
        public void Start()
        {
            _speedDate.Start();
            Spawn = _speedDate.PluginProver.Get<SpawnerPlugin>();
        }

        private void SpeedDateOnStarted()
        {

            Spawn.RegisterSpawner(new SpawnerOptions(), (controller, error) =>
            {
                System.Console.WriteLine(error);
            });
        }
    }
}
