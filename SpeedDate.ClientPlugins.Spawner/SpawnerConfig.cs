using System;
using SpeedDate.Configuration;

namespace SpeedDate.ClientPlugins.Spawner
{
        public class SpawnerConfig : IConfig
        {
            public string MachineIp { get; set; } = "127.0.0.1";
            public bool SpawnInBatchmode {get; set; }
            public string ExecutablePath {get; set; } = string.Empty;
            public bool AddWebGlFlag { get; set; }
        }
}
