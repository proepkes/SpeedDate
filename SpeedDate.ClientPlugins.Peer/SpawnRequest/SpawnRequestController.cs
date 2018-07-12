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

        public SpawnStatus Status { get; private set; }

        private readonly SpawnRequestPlugin _spawnServer;

        public SpawnRequestController(SpawnRequestPlugin owner, int spawnId, IClient client)
        {
            _spawnServer = owner;

            _client = client;
            SpawnId = spawnId;

            // Set handlers
            client.SetHandler((ushort) OpCodes.SpawnRequestStatusChange, HandleStatusUpdate);
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

        private void HandleStatusUpdate(IIncommingMessage message)
        {
            var data = message.Deserialize<SpawnStatusUpdatePacket>();

            var controller = _spawnServer.GetRequestController(data.SpawnId);

            if (controller == null)
                return;

            controller.Status = data.Status;

            controller.StatusChanged?.Invoke(data.Status);
        }
    }
}
