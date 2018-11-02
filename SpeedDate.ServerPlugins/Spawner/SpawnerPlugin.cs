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
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;

namespace SpeedDate.ServerPlugins.Spawner
{
    internal sealed class SpawnerPlugin : SpeedDateServerPlugin
    {
        [Inject] private readonly ILogger _logger;
        [Inject] private readonly SpawnerConfig _config;

        private readonly Dictionary<int, SpawnTask> _spawnTasks = new Dictionary<int, SpawnTask>();
        private readonly Dictionary<int, RegisteredSpawner> _spawners = new Dictionary<int, RegisteredSpawner>();

        private int _spawnerId;
        private int _spawnTaskId;

        public override void Loaded()
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

        public SpawnTask Spawn(Dictionary<string, string> properties, string region = "", string customArgs = "")
        {
            var spawners = GetSpawners(region, properties);

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
            return availableSpawner == null ? null : Spawn(availableSpawner, properties, customArgs);
        }

        /// <summary>
        /// Requests a specific spawner to spawn a process
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="customArgs"></param>
        /// <param name="spawner"></param>
        /// <returns></returns>
        public SpawnTask Spawn(RegisteredSpawner spawner, Dictionary<string, string> properties, string customArgs)
        {
            var task = new SpawnTask(GenerateSpawnTaskId(), spawner, properties, customArgs);

            _spawnTasks[task.SpawnId] = task;

            spawner.AddTaskToQueue(task);

            _logger.Debug("Spawner was found, and spawn task created: " + task);

            return task;
        }

        private RegisteredSpawner CreateSpawner(IPeer peer, SpawnerOptions options)
        {
            var spawner = new RegisteredSpawner(GenerateSpawnerId(), peer, options);

            // If this is the first time registering a spawner...
            if (!(peer.GetProperty((int) PeerPropertyKeys.RegisteredSpawners) is Dictionary<int, RegisteredSpawner>
                peerSpawners))
            {
                //... save the dictionary
                peerSpawners = new Dictionary<int, RegisteredSpawner>();
                peer.SetProperty((int) PeerPropertyKeys.RegisteredSpawners, peerSpawners);

                peer.Disconnected += OnRegisteredPeerDisconnect;
            }

            // Add a new spawner
            peerSpawners[spawner.SpawnerId] = spawner;

            // Add the spawner to a list of all spawners
            _spawners[spawner.SpawnerId] = spawner;

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

        private void DestroySpawner(RegisteredSpawner spawner)
        {
            var peer = spawner.Peer;

            if (peer != null &&
                peer.GetProperty((int) PeerPropertyKeys.RegisteredSpawners) is Dictionary<int, RegisteredSpawner>
                    peerRooms) peerRooms.Remove(spawner.SpawnerId);

            _spawners.Remove(spawner.SpawnerId);

            _logger.Info($"Spawner disconnected. ID: {spawner.SpawnerId}");
        }

        private int GenerateSpawnerId()
        {
            return _spawnerId++;
        }

        private int GenerateSpawnTaskId()
        {
            return _spawnTaskId++;
        }
         
        private List<RegisteredSpawner> GetSpawners(string region = null, Dictionary<string, string> properties = null)
        {
            // If region is not provided, retrieve all spawners
            return string.IsNullOrEmpty(region) ? _spawners.Values.ToList() : _spawners.Values.Where(s => s.Options.Region == region).ToList();
        }

        private bool HasCreationPermissions(IPeer peer)
        {
            var extension = peer.GetExtension<PeerSecurityExtension>();

            return extension.PermissionLevel >= _config.CreateSpawnerPermissionLevel;
        }

        private bool CanClientSpawn(IPeer peer, ClientsSpawnRequestPacket data)
        {
            return !_config.SpawnRequestsRequireAuthentication || peer.HasExtension<UserExtension>();
        }

        private async void StartQueueUpdater()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_config.QueueUpdateFrequency));

                try
                {
                    foreach (var spawner in _spawners.Values)
                        spawner.UpdateQueue();
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    return;
                }
            }
        }

        private void HandleClientsSpawnRequest(IIncommingMessage message)
        {
            var data = message.Deserialize<ClientsSpawnRequestPacket>();
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
                message.Peer.SendMessage((ushort)OpCodes.SpawnRequestStatusChange, new SpawnStatusUpdatePacket
                {
                    SpawnId = task.SpawnId,
                    Status = status
                });
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

            if (prevRequest.Status >= SpawnStatus.Finalized)
            {
                message.Respond("You can't abort a completed request", ResponseStatus.Failed);
                return;
            }

            if (prevRequest.Status <= SpawnStatus.None)
            {
                message.Respond("Already aborting", ResponseStatus.Success);
                return;
            }

            prevRequest.Kill();
        }

        private void HandleGetCompletionData(IIncommingMessage message)
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

        private void HandleRegisterSpawner(IIncommingMessage message)
        {
            if (!HasCreationPermissions(message.Peer))
            {
                message.Respond("Insufficient permissions", ResponseStatus.Unauthorized);
                return;
            }

            var options = message.Deserialize<SpawnerOptions>();

            var spawner = CreateSpawner(message.Peer, options);

            _logger.Info($"New Spawner registered. ID: {spawner.SpawnerId}. Region: {options.Region}");

            // Respond with spawner id
            message.Respond(spawner.SpawnerId, ResponseStatus.Success);
        }

        /// <summary>
        ///     Handles a message from spawned process. Spawned process send this message to notify server that it was started
        /// </summary>
        /// <param name="message"></param>
        private void HandleRegisterSpawnedProcess(IIncommingMessage message)
        {
            var data = message.Deserialize<RegisterSpawnedProcessPacket>();

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

            message.Respond(task.Properties.ToBytes(), ResponseStatus.Success);
        }

        private void HandleCompleteSpawnProcess(IIncommingMessage message)
        {
            var data = message.Deserialize<SpawnFinalizationPacket>();

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

        private void HandleProcessKilled(IIncommingMessage message)
        {
            var spawnId = message.AsInt();

            _spawnTasks.TryGetValue(spawnId, out var task);

            if (task == null)
                return;

            task.OnProcessKilled();
            task.Spawner.OnProcessKilled();
        }

        private void HandleProcessStarted(IIncommingMessage message)
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
            var packet = message.Deserialize<IntPairPacket>();

            _spawners.TryGetValue(packet.A, out var spawner);

            spawner?.UpdateProcessesCount(packet.B);
        }
    }
}
