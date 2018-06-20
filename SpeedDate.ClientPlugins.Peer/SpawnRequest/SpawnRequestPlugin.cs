using System.Collections.Generic;

using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.Peer.SpawnRequest
{
    public delegate void ClientSpawnRequestCallback(SpawnRequestController controller);

    public class SpawnRequestPlugin : SpeedDateClientPlugin
    {
        public delegate void AbortSpawnHandler();

        public delegate void FinalizationDataHandler(Dictionary<string, string> data);

        private readonly Dictionary<int, SpawnRequestController> _localSpawnRequests;

        public SpawnRequestPlugin()
        {
            _localSpawnRequests = new Dictionary<int, SpawnRequestController>();
        }
        
        /// <summary>
        /// Sends a request to master server, to spawn a process in a given region, and with given options
        /// </summary>
        public void RequestSpawn(Dictionary<string, string> options, string region, ClientSpawnRequestCallback callback, ErrorCallback errorCallback)
        {
            if (!Connection.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            var packet = new ClientsSpawnRequestPacket()
            {
                Options = options,
                Region = region
            };

            Connection.SendMessage((ushort) OpCodes.ClientsSpawnRequest, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                // Spawn id
                var spawnId = response.AsInt();

                var controller = new SpawnRequestController(this, spawnId, Connection, options);

                _localSpawnRequests[controller.SpawnId] = controller;

                callback.Invoke(controller);
            });
        }

        /// <summary>
        /// Sends a request to abort spawn request, which was not yet finalized
        /// </summary>
        /// <param name="spawnId"></param>
        public void AbortSpawn(int spawnId)
        {
            AbortSpawn(spawnId, () =>
            {
            }, error =>
            {
                if (error != null)
                    Logs.Error(error);
            });
        }

        /// <summary>
        /// Sends a request to abort spawn request, which was not yet finalized
        /// </summary>
        public void AbortSpawn(int spawnId, AbortSpawnHandler callback, ErrorCallback errorCallback)
        {
            if (!Connection.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            Connection.SendMessage((ushort)OpCodes.AbortSpawnRequest, spawnId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke();
            });
        }

        /// <summary>
        /// Retrieves data, which was given to master server by a spawned process,
        /// which was finalized
        /// </summary>
        public void GetFinalizationData(int spawnId, FinalizationDataHandler callback, ErrorCallback errorCallback, IClientSocket connection)
        {
            if (!connection.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            connection.SendMessage((ushort)OpCodes.GetSpawnFinalizationData, spawnId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(new Dictionary<string, string>().FromBytes(response.AsBytes()));
            });
        }

        /// <summary>
        /// Retrieves a specific spawn request controller
        /// </summary>
        /// <param name="spawnId"></param>
        /// <returns></returns>
        public SpawnRequestController GetRequestController(int spawnId)
        {
            _localSpawnRequests.TryGetValue(spawnId, out var controller);

            return controller;
        }
    }
}
