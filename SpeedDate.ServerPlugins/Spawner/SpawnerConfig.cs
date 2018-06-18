using SpeedDate.Configuration;

namespace SpeedDate.ServerPlugins.Spawner
{
    class SpawnerConfig : IConfig
    {
        public int CreateSpawnerPermissionLevel { get; set; }

        public bool EnableClientSpawnRequests { get; set; }

        public int QueueUpdateFrequency { get; set; }
    }
}
