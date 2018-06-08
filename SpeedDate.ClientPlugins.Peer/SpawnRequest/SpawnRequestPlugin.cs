using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.Peer.SpawnRequest
{
    public delegate void ClientSpawnRequestCallback(SpawnRequestController controller, string error);

    public class SpawnRequestPlugin : SpeedDateClientPlugin
    {
        public delegate void AbortSpawnHandler(bool isSuccessful, string error);

        public delegate void FinalizationDataHandler(Dictionary<string, string> data, string error);

        private readonly Dictionary<int, SpawnRequestController> _localSpawnRequests;

        public SpawnRequestPlugin(IClientSocket connection) : base(connection)
        {
            _localSpawnRequests = new Dictionary<int, SpawnRequestController>();
        }
        
        /// <summary>
        /// Sends a request to master server, to spawn a process in a given region, and with given options
        /// </summary>
        public void RequestSpawn(Dictionary<string, string> options, string region, ClientSpawnRequestCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            var packet = new ClientsSpawnRequestPacket()
            {
                Options = options,
                Region = region
            };

            Connection.SendMessage((short) OpCodes.ClientsSpawnRequest, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown error"));
                    return;
                }

                // Spawn id
                var spawnId = response.AsInt();

                var controller = new SpawnRequestController(this, spawnId, Connection, options);

                _localSpawnRequests[controller.SpawnId] = controller;

                callback.Invoke(controller, null);
            });
        }

        /// <summary>
        /// Sends a request to abort spawn request, which was not yet finalized
        /// </summary>
        /// <param name="spawnId"></param>
        public void AbortSpawn(int spawnId)
        {
            AbortSpawn(spawnId, (successful, error) =>
            {
                if (error != null)
                    Logs.Error(error);
            }, Connection);
        }

        /// <summary>
        /// Sends a request to abort spawn request, which was not yet finalized
        /// </summary>
        public void AbortSpawn(int spawnId, AbortSpawnHandler callback)
        {
            AbortSpawn(spawnId, callback, Connection);
        }

        /// <summary>
        /// Sends a request to abort spawn request, which was not yet finalized
        /// </summary>
        public void AbortSpawn(int spawnId, AbortSpawnHandler callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            connection.SendMessage((short)OpCodes.AbortSpawnRequest, spawnId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Retrieves data, which was given to master server by a spawned process,
        /// which was finalized
        /// </summary>
        /// <param name="spawnId"></param>
        /// <param name="callback"></param>
        public void GetFinalizationData(int spawnId, FinalizationDataHandler callback)
        {
            GetFinalizationData(spawnId, callback, Connection);
        }

        /// <summary>
        /// Retrieves data, which was given to master server by a spawned process,
        /// which was finalized
        /// </summary>
        public void GetFinalizationData(int spawnId, FinalizationDataHandler callback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            connection.SendMessage((short)OpCodes.GetSpawnFinalizationData, spawnId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(new Dictionary<string, string>().FromBytes(response.AsBytes()), null);
            });
        }

        /// <summary>
        /// Retrieves a specific spawn request controller
        /// </summary>
        /// <param name="spawnId"></param>
        /// <returns></returns>
        public SpawnRequestController GetRequestController(int spawnId)
        {
            SpawnRequestController controller;
            _localSpawnRequests.TryGetValue(spawnId, out controller);

            return controller;
        }
    }
}