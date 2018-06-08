using System;
using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.Peer.SpawnRequest
{
    public class SpawnRequestController
    {
        private readonly IClientSocket _connection;
        public int SpawnId { get; set; }

        public event Action<SpawnStatus> StatusChanged;

        public SpawnStatus Status { get; private set; }

        /// <summary>
        /// A dictionary of options that user provided when requesting a 
        /// process to be spawned
        /// </summary>
        public Dictionary<string, string> SpawnOptions;

        private readonly SpawnRequestPlugin _spawnServer;

        public SpawnRequestController(SpawnRequestPlugin owner, int spawnId, IClientSocket connection, Dictionary<string, string> spawnOptions)
        {
            _spawnServer = owner;

            _connection = connection;
            SpawnId = spawnId;
            SpawnOptions = spawnOptions;

            // Set handlers
            connection.SetHandler((short) OpCodes.SpawnRequestStatusChange, HandleStatusUpdate);
        }

        public void Abort()
        {
            _spawnServer.AbortSpawn(SpawnId);
        }

        public void Abort(SpawnRequestPlugin.AbortSpawnHandler handler)
        {
            _spawnServer.AbortSpawn(SpawnId, handler);
        }

        public void GetFinalizationData(SpawnRequestPlugin.FinalizationDataHandler handler)
        {
            _spawnServer.GetFinalizationData(SpawnId, handler, _connection);
        }

        private void HandleStatusUpdate(IIncommingMessage message)
        {
            var data = message.Deserialize(new SpawnStatusUpdatePacket());

            var controller = _spawnServer.GetRequestController(data.SpawnId);

            if (controller == null)
                return;

            controller.Status = data.Status;

            controller.StatusChanged?.Invoke(data.Status);
        }
    }
}