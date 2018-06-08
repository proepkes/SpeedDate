using System;
using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;

namespace SpeedDate.ClientPlugins.GameServer
{
    public class SpawnTaskController
    {
        private readonly IClientSocket _connection;
        public int SpawnId { get; private set; }
        public Dictionary<string, string> Properties { get; private set; }

        private readonly RoomsPlugin _spawnerClient;

        public SpawnTaskController(RoomsPlugin owner, int spawnId, Dictionary<string, string> properties, IClientSocket connection)
        {
            _spawnerClient = owner;

            _connection = connection;
            SpawnId = spawnId;
            Properties = properties;
        }

        public void FinalizeTask(Dictionary<string, string> finalizationData, Action callback)
        {
            _spawnerClient.FinalizeSpawnedProcess(SpawnId, (successful, error) =>
            {
                if (error != null)
                    Logs.Error("Error while completing the spawn task: " + error);

                callback.Invoke();
            }, finalizationData);
        }
    }
}