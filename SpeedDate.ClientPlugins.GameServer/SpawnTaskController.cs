using System;
using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Logging;

namespace SpeedDate.ClientPlugins.GameServer
{
    public class SpawnTaskController
    {
        public int SpawnId { get; private set; }
        public Dictionary<string, string> Properties { get; private set; }

        private readonly RoomsPlugin _spawnerClient;

        public SpawnTaskController(RoomsPlugin owner, int spawnId, Dictionary<string, string> properties)
        {
            _spawnerClient = owner;

            SpawnId = spawnId;
            Properties = properties;
        }

        public void FinalizeTask(Dictionary<string, string> finalizationData, Action callback)
        {
            _spawnerClient.FinalizeSpawnedProcess(SpawnId, callback.Invoke, error =>
            {
                if (error != null)
                    Logs.Error("Error while completing the spawn task: " + error);
            }, finalizationData);
        }
    }
}
