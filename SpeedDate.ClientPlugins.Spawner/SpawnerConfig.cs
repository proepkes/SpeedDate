using SpeedDate.Configuration;

namespace SpeedDate.ClientPlugins.Spawner
{
        public class SpawnerConfig : IConfig
        {
            public string MachineIp { get; set; }
            public bool SpawnInBatchmode {get; set; }
            public string ExecutablePath {get; set; }
            public bool AddWebGlFlag { get; set; }
        }
}
