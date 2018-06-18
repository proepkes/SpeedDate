using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Common;
using SpeedDate.Packets.Spawner;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.Server;

namespace SpeedDate.ServerPlugins.Spawner
{
    internal class SpawnerPlugin : SpeedDateServerPlugin
    {
        public delegate void SpawnedProcessRegistrationHandler(SpawnTask task, IPeer peer);

        [Inject]
        private readonly ILogger _logger;

        [Inject]
        private readonly SpawnerConfig _config;

        private readonly Dictionary<int, RegisteredSpawner> _spawners = new Dictionary<int, RegisteredSpawner>();

        private readonly Dictionary<int, SpawnTask> _spawnTasks = new Dictionary<int, SpawnTask>();

        private int _spawnerId;
        private int _spawnTaskId;

        public override void Loaded(IPluginProvider pluginProvider)
        {
            // Add handlers
            Server.SetHandler(OpCodes.RegisterSpawner, HandleRegisterSpawner);
            Server.SetHandler(OpCodes.ClientsSpawnRequest, HandleClientsSpawnRequest);
            Server.SetHandler(OpCodes.RegisterSpawnedProcess, HandleRegisterSpawnedProcess);
            Server.SetHandler(OpCodes.CompleteSpawnProcess, HandleCompleteSpawnProcess);
            Server.SetHandler(OpCodes.ProcessStarted, HandleProcessStarted);
            Server.SetHandler(OpCodes.ProcessKilled, HandleProcessKilled);
            Server.SetHandler(OpCodes.AbortSpawnRequest, HandleAbortSpawnRequest);
            Server.SetHandler(OpCodes.GetSpawnFinalizationData, HandleGetCompletionData);
            Server.SetHandler(OpCodes.UpdateSpawnerProcessesCount, HandleSpawnedProcessesCount);

            Task.Factory.StartNew(StartQueueUpdater, TaskCreationOptions.LongRunning);
        }

        public event Action<RegisteredSpawner> SpawnerRegistered;
        public event Action<RegisteredSpawner> SpawnerDestroyed;
        public event SpawnedProcessRegistrationHandler SpawnedProcessRegistered;


        public virtual RegisteredSpawner CreateSpawner(IPeer peer, SpawnerOptions options)
        {
            var spawner = new RegisteredSpawner(GenerateSpawnerId(), peer, options);

            if (!(peer.GetProperty((int) PeerPropertyKeys.RegisteredSpawners) is Dictionary<int, RegisteredSpawner>
                peerSpawners))
            {
                // If this is the first time registering a spawners

                // Save the dictionary
                peerSpawners = new Dictionary<int, RegisteredSpawner>();
                peer.SetProperty((int) PeerPropertyKeys.RegisteredSpawners, peerSpawners);

                peer.Disconnected += OnRegisteredPeerDisconnect;
            }

            // Add a new spawner
            peerSpawners[spawner.SpawnerId] = spawner;

            // Add the spawner to a list of all spawners
            _spawners[spawner.SpawnerId] = spawner;

            // Invoke the event
            SpawnerRegistered?.Invoke(spawner);

            return spawner;
        }

        private void OnRegisteredPeerDisconnect(IPeer peer)
        {
            if (!(peer.GetProperty((int) PeerPropertyKeys.RegisteredSpawners) is Dictionary<int, RegisteredSpawner>
                peerSpawners))
                return;

            // Create a copy so that we can iterate safely
            var registeredSpawners = peerSpawners.Values.ToList();

            foreach (var registeredSpawner in registeredSpawners) DestroySpawner(registeredSpawner);
        }

        public void DestroySpawner(RegisteredSpawner spawner)
        {
            var peer = spawner.Peer;

            if (peer != null &&
                peer.GetProperty((int) PeerPropertyKeys.RegisteredSpawners) is Dictionary<int, RegisteredSpawner>
                    peerRooms) peerRooms.Remove(spawner.SpawnerId);

            // Remove the spawner from all spawners
            _spawners.Remove(spawner.SpawnerId);

            _logger.Info($"Spawner disconnected. ID: {spawner.SpawnerId}");
            // Invoke the event
            SpawnerDestroyed?.Invoke(spawner);
        }

        public int GenerateSpawnerId()
        {
            return _spawnerId++;
        }

        public int GenerateSpawnTaskId()
        {
            return _spawnTaskId++;
        }

        public virtual SpawnTask Spawn(Dictionary<string, string> properties, string region = "",
            string customArgs = "")
        {
            var spawners = GetFilteredSpawners(properties, region);

            if (spawners.Count <= 0)
            {
                _logger.Warn("No spawner was returned after filtering. " +
                             (string.IsNullOrEmpty(region) ? "" : "Region: " + region));
                return null;
            }

            // Order from least busy server
            var orderedSpawners = spawners.OrderByDescending(s => s.CalculateFreeSlotsCount());
            var availableSpawner = orderedSpawners.FirstOrDefault(s => s.CanSpawnAnotherProcess());

            // Ignore, if all of the spawners are busy
            return availableSpawner == null ? null : Spawn(properties, customArgs, availableSpawner);
        }

        /// <summary>
        ///     Requests a specific spawner to spawn a process
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="customArgs"></param>
        /// <param name="spawner"></param>
        /// <returns></returns>
        public virtual SpawnTask Spawn(Dictionary<string, string> properties, string customArgs,
            RegisteredSpawner spawner)
        {
            var task = new SpawnTask(GenerateSpawnTaskId(), spawner, properties, customArgs);

            _spawnTasks[task.SpawnId] = task;

            spawner.AddTaskToQueue(task);

            _logger.Debug("Spawner was found, and spawn task created: " + task);

            return task;
        }

        /// <summary>
        ///     Retrieves a list of spawner that can be used with given properties and region name
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public virtual List<RegisteredSpawner> GetFilteredSpawners(Dictionary<string, string> properties, string region)
        {
            return GetSpawners(region);
        }

        public virtual List<RegisteredSpawner> GetSpawners()
        {
            return GetSpawners(null);
        }

        public virtual List<RegisteredSpawner> GetSpawners(string region)
        {
            // If region is not provided, retrieve all spawners
            return string.IsNullOrEmpty(region) ? _spawners.Values.ToList() : GetSpawnersInRegion(region);
        }

        public virtual List<RegisteredSpawner> GetSpawnersInRegion(string region)
        {
            return _spawners.Values
                .Where(s => s.Options.Region == region)
                .ToList();
        }

        /// <summary>
        ///     Returns true, if peer has permissions to register a spawner
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        protected virtual bool HasCreationPermissions(IPeer peer)
        {
            var extension = peer.GetExtension<PeerSecurityExtension>();

            return extension.PermissionLevel >= _config.CreateSpawnerPermissionLevel;
        }

        protected virtual bool CanClientSpawn(IPeer peer, ClientsSpawnRequestPacket data)
        {
            return _config.EnableClientSpawnRequests;
        }

        protected virtual async void StartQueueUpdater()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_config.QueueUpdateFrequency));

                foreach (var spawner in _spawners.Values)
                    try
                    {
                        spawner.UpdateQueue();
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                        return;
                    }
            }
        }

        #region Message Handlers

        protected virtual void HandleClientsSpawnRequest(IIncommingMessage message)
        {
            var data = message.Deserialize(new ClientsSpawnRequestPacket());
            var peer = message.Peer;

            if (!CanClientSpawn(peer, data))
            {
                // Client can't spawn
                message.Respond("Unauthorized", ResponseStatus.Unauthorized);
                return;
            }

            if (peer.GetProperty((int) PeerPropertyKeys.ClientSpawnRequest) is SpawnTask prevRequest &&
                !prevRequest.IsDoneStartingProcess)
            {
                // Client has unfinished request
                message.Respond("You already have an active request", ResponseStatus.Failed);
                return;
            }

            // Get the spawn task
            var task = Spawn(data.Options, data.Region);

            if (task == null)
            {
                message.Respond("All the servers are busy. Try again later".ToBytes(), ResponseStatus.Failed);
                return;
            }

            task.Requester = message.Peer;

            // Save the task
            peer.SetProperty((int) PeerPropertyKeys.ClientSpawnRequest, task);

            // Listen to status changes
            task.StatusChanged += status =>
            {
                // Send status update
                var msg = MessageHelper.Create((ushort) OpCodes.SpawnRequestStatusChange, new SpawnStatusUpdatePacket
                {
                    SpawnId = task.SpawnId,
                    Status = status
                });
                message.Peer.SendMessage(msg);
            };

            message.Respond(task.SpawnId, ResponseStatus.Success);
        }

        private void HandleAbortSpawnRequest(IIncommingMessage message)
        {
            if (!(message.Peer.GetProperty((int) PeerPropertyKeys.ClientSpawnRequest) is SpawnTask prevRequest))
            {
                message.Respond("There's nothing to abort", ResponseStatus.Failed);
                return;
            }

            if (prevRequest.Status >= Packets.Spawner.SpawnStatus.Finalized)
            {
                message.Respond("You can't abort a completed request", ResponseStatus.Failed);
                return;
            }

            if (prevRequest.Status <= Packets.Spawner.SpawnStatus.None)
            {
                message.Respond("Already aborting", ResponseStatus.Success);
                return;
            }

            prevRequest.Abort();
        }

        protected virtual void HandleGetCompletionData(IIncommingMessage message)
        {
            var spawnId = message.AsInt();

            _spawnTasks.TryGetValue(spawnId, out var task);

            if (task == null)
            {
                message.Respond("Invalid request", ResponseStatus.Failed);
                return;
            }

            if (task.Requester != message.Peer)
            {
                message.Respond("You're not the requester", ResponseStatus.Unauthorized);
                return;
            }

            if (task.FinalizationPacket == null)
            {
                message.Respond("Task has no completion data", ResponseStatus.Failed);
                return;
            }

            // Respond with data (dictionary of strings)
            message.Respond(task.FinalizationPacket.FinalizationData.ToBytes(), ResponseStatus.Success);
        }

        protected virtual void HandleRegisterSpawner(IIncommingMessage message)
        {
            if (!HasCreationPermissions(message.Peer))
            {
                message.Respond("Insufficient permissions", ResponseStatus.Unauthorized);
                return;
            }

            var options = message.Deserialize(new SpawnerOptions());

            var spawner = CreateSpawner(message.Peer, options);

            _logger.Info($"New Spawner registered. ID: {spawner.SpawnerId}");

            // Respond with spawner id
            message.Respond(spawner.SpawnerId, ResponseStatus.Success);
        }

        /// <summary>
        ///     Handles a message from spawned process. Spawned process send this message
        ///     to notify server that it was started
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleRegisterSpawnedProcess(IIncommingMessage message)
        {
            var data = message.Deserialize(new RegisterSpawnedProcessPacket());

            _spawnTasks.TryGetValue(data.SpawnId, out var task);

            if (task == null)
            {
                message.Respond("Invalid spawn task", ResponseStatus.Failed);
                _logger.Error("Process tried to register to an unknown task");
                return;
            }

            if (task.UniqueCode != data.SpawnCode)
            {
                message.Respond("Unauthorized", ResponseStatus.Unauthorized);
                _logger.Error("Spawned process tried to register, but failed due to mismaching unique code");
                return;
            }

            task.OnRegistered(message.Peer);

            SpawnedProcessRegistered?.Invoke(task, message.Peer);

            message.Respond(task.Properties.ToBytes(), ResponseStatus.Success);
        }

        protected virtual void HandleCompleteSpawnProcess(IIncommingMessage message)
        {
            var data = message.Deserialize(new SpawnFinalizationPacket());

            _spawnTasks.TryGetValue(data.SpawnId, out var task);

            if (task == null)
            {
                message.Respond("Invalid spawn task", ResponseStatus.Failed);
                _logger.Error("Process tried to complete to an unknown task");
                return;
            }

            if (task.RegisteredPeer != message.Peer)
            {
                message.Respond("Unauthorized", ResponseStatus.Unauthorized);
                _logger.Error(
                    "Spawned process tried to complete spawn task, but it's not the same peer who registered to the task");
                return;
            }

            task.OnFinalized(data);

            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleProcessKilled(IIncommingMessage message)
        {
            var spawnId = message.AsInt();

            _spawnTasks.TryGetValue(spawnId, out var task);

            if (task == null)
                return;

            task.OnProcessKilled();
            task.Spawner.OnProcessKilled();
        }

        protected virtual void HandleProcessStarted(IIncommingMessage message)
        {
            var spawnId = message.AsInt();

            _spawnTasks.TryGetValue(spawnId, out var task);

            if (task == null)
                return;

            task.OnProcessStarted();
            task.Spawner.OnProcessStarted();
        }

        private void HandleSpawnedProcessesCount(IIncommingMessage message)
        {
            var packet = message.Deserialize(new IntPairPacket());

            _spawners.TryGetValue(packet.A, out var spawner);

            spawner?.UpdateProcessesCount(packet.B);
        }

        #endregion
    }
}
