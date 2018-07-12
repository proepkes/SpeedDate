using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Common;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.Spawner
{
    public delegate void RegisterSpawnerCallback(int spawnerId);

    public class SpawnerPlugin : SpeedDateClientPlugin
    {
        [Inject] private ILogger _logger;
        [Inject] private SpawnerConfig _config;

        private ISpawnerRequestsDelegate _spawnerRequestsDelegate;

        public int SpawnerId { get; private set; }

        public override void Loaded()
        {
            _spawnerRequestsDelegate = new ProcessSpawnerRequestHandler(Client, _config);

            Client.SetHandler((ushort)OpCodes.SpawnRequest, HandleSpawnRequest);
            Client.SetHandler((ushort)OpCodes.KillSpawnedProcess, HandleKillSpawnedProcessRequest);
        }

        /// <summary>
        /// Sends a request to master server, to register an existing spawner with given options
        /// </summary>
        public void Register(RegisterSpawnerCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            _logger.Info("Registering Spawner...");
            Client.SendMessage((ushort) OpCodes.RegisterSpawner, new SpawnerOptions
            {
                MaxProcesses = _config.MaxProcesses,
                Region = _config.Region
            }, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown Error"));
                    return;
                }

                SpawnerId = response.AsInt();

                callback.Invoke(SpawnerId);
            });
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Notifies master server, how many processes are running on this spawner. </summary>
        ///
        /// <param name="spawnerId">    Identifier for the spawner. </param>
        /// <param name="count">        Number of processes. </param>
        ///-------------------------------------------------------------------------------------------------
        public void UpdateProcessesCount(int spawnerId, int count)
        {
            var packet = new IntPairPacket
            {
                A = spawnerId,
                B = count
            };
            Client.SendMessage((ushort)OpCodes.UpdateSpawnerProcessesCount, packet);
        }

        public void SetSpawnerRequestsDelegate(ISpawnerRequestsDelegate handler)
        {
            _spawnerRequestsDelegate = handler;
        }
        
        private void HandleSpawnRequest(IIncommingMessage message)
        {
            var packet = message.Deserialize<SpawnRequestPacket>();
            if (packet == null)
            {
                message.Respond(ResponseStatus.Error);
                return;
            }
            // Pass the request to handler
            _spawnerRequestsDelegate.HandleSpawnRequest(message, packet);
        }

        private void HandleKillSpawnedProcessRequest(IIncommingMessage message)
        {
            var data = message.Deserialize<KillSpawnedProcessPacket>();
            if (data == null)
            {
                message.Respond(ResponseStatus.Error);
                return;
            }

            message.Respond(_spawnerRequestsDelegate.HandleKillRequest(data.SpawnId) ? ResponseStatus.Success : ResponseStatus.Failed);
        }
    }
}
