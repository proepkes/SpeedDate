using System;
using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.Peer.SpawnRequest
{
    public class SpawnRequestController
    {
        private readonly IClient _client;
        public int SpawnId { get; }

        public event Action<SpawnStatus> StatusChanged;

        private SpawnStatus status;

        public SpawnStatus Status
        {
            get => status;
            set
            {
                if (status != value)
                {
                    status = value;
                    StatusChanged?.Invoke(Status);
                }
            }
        }

        private readonly SpawnRequestPlugin _spawnServer;

        public SpawnRequestController(SpawnRequestPlugin owner, int spawnId, IClient client)
        {
            _spawnServer = owner;

            _client = client;
            SpawnId = spawnId;

        }

        public void Abort()
        {
            _spawnServer.AbortSpawn(SpawnId);
        }

        public void Abort(SpawnRequestPlugin.AbortSpawnHandler handler, ErrorCallback errorCallback)
        {
            _spawnServer.AbortSpawn(SpawnId, handler, errorCallback);
        }

        public void GetFinalizationData(SpawnRequestPlugin.FinalizationDataHandler handler, ErrorCallback errorCallback)
        {
            _spawnServer.GetFinalizationData(SpawnId, handler, errorCallback,  _client);
        }
    }
}
