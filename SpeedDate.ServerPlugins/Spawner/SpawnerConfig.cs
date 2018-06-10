using SpeedDate.Configuration;

namespace SpeedDate.ServerPlugins.Spawner
{
    [PluginConfiguration(typeof(SpawnerPlugin))]
    class SpawnerConfig
    {
        public int CreateSpawnerPermissionLevel { get; set; }

        public bool EnableClientSpawnRequests { get; set; }

        public int QueueUpdateFrequency { get; set; }
    }
}
