using System;
using System.Collections.Generic;

using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Common;
using SpeedDate.Packets.Spawner;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.ClientPlugins.Spawner
{
    public delegate void RegisterSpawnerCallback(SpawnerController spawner, string error);

    public class SpawnerPlugin : SpeedDateClientPlugin
    {
        [Inject]
        private ILogger _logger;
        
        private readonly Dictionary<int, SpawnerController> _locallyCreatedSpawners;

        public const int PortsStartFrom = 10000;

        private readonly Queue<int> _freePorts;
        private int _lastPortTaken = -1;

        /// <summary>
        /// If true, this process is considered to be spawned by the spawner
        /// </summary>
        public bool IsSpawnedProccess { get; }

        /// <summary>
        /// Invoked on "spawner server", when it successfully registers to master server
        /// </summary>
        public event Action<SpawnerController> SpawnerRegistered;
        
        public SpawnerPlugin() 
        {
            _locallyCreatedSpawners = new Dictionary<int, SpawnerController>();
            _freePorts = new Queue<int>();

            IsSpawnedProccess = CommandLineArgs.IsProvided(CommandLineArgs.Names.SpawnCode);
        }

        /// <summary>
        /// Sends a request to master server, to register an existing spawner with given options
        /// </summary>
        public void RegisterSpawner(SpawnerOptions options, RegisterSpawnerCallback callback)
        {
            _logger.Info("Registering Spawner...");
            if (!Connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            Connection.SendMessage((short) OpCodes.RegisterSpawner, options, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                var spawnerId = response.AsInt();

                var controller = new SpawnerController(this, spawnerId, Connection, options);

                // Save reference
                _locallyCreatedSpawners[spawnerId] = controller;

                callback.Invoke(controller, null);
                
                // Invoke the event
                SpawnerRegistered?.Invoke(controller);
            });
        }

        /// <summary>
        /// Notifies master server, how many processes are running on a specified spawner
        /// </summary>
        public void UpdateProcessesCount(int spawnerId, int count)
        {
            var packet = new IntPairPacket()
            {
                A = spawnerId,
                B = count
            };
            Connection.SendMessage((short)OpCodes.UpdateSpawnerProcessesCount, packet);
        }

        public SpawnerController GetController(int spawnerId)
        {
            _locallyCreatedSpawners.TryGetValue(spawnerId, out var controller);

            return controller;
        }

        public IEnumerable<SpawnerController> GetLocallyCreatedSpawners()
        {
            return _locallyCreatedSpawners.Values;
        }

        public int GetAvailablePort()
        {
            // Return a port from a list of available ports
            if (_freePorts.Count > 0)
                return _freePorts.Dequeue();

            if (_lastPortTaken < 0)
                _lastPortTaken = PortsStartFrom;

            return _lastPortTaken++;
        }

        public void ReleasePort(int port)
        {
            _freePorts.Enqueue(port);
        }

        /// <summary>
        /// Should be called by a spawned process, as soon as it is started
        /// </summary>
        /// <param name="spawnId"></param>
        /// <param name="processId"></param>
        /// <param name="cmdArgs"></param>
        public void NotifyProcessStarted(int spawnId, int processId, string cmdArgs)
        {
            if (!Connection.IsConnected)
                return;

            Connection.SendMessage((short)OpCodes.ProcessStarted, new SpawnedProcessStartedPacket()
            {
                CmdArgs = cmdArgs,
                ProcessId = processId,
                SpawnId = spawnId
            });
        }

        public void NotifyProcessKilled(int spawnId)
        {
            if (!Connection.IsConnected)
                return;

            Connection.SendMessage((short)OpCodes.ProcessKilled, spawnId);
        }
    }
}
