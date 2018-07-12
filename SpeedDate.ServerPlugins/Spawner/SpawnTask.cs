using System;
using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ServerPlugins.Spawner
{
    /// <summary>
    /// Represents a spawn request, and manages the state of request
    /// from start to finalization
    /// </summary>
    public class SpawnTask
    {
        public RegisteredSpawner Spawner { get; }
        public Dictionary<string, string> Properties { get; }
        public string CustomArgs { get; }

        public int SpawnId { get; }
        public event Action<SpawnStatus> StatusChanged;

        private SpawnStatus _status;

        public string UniqueCode { get; }

        public SpawnFinalizationPacket FinalizationPacket { get; private set; }

        private readonly List<Action<SpawnTask>> _whenDoneCallbacks;

        public SpawnTask(int spawnId, RegisteredSpawner spawner, 
            Dictionary<string, string> properties, string customArgs) {

            SpawnId = spawnId;

            Spawner = spawner;
            Properties = properties;
            CustomArgs = customArgs;

            UniqueCode = Util.CreateRandomString(6);
            _whenDoneCallbacks = new List<Action<SpawnTask>>();
        }

        public bool IsAborted => _status < SpawnStatus.None;

        public bool IsDoneStartingProcess => IsAborted || IsProcessStarted;

        public bool IsProcessStarted => Status >= SpawnStatus.WaitingForProcess;

        public SpawnStatus Status
        {
            get => _status;
            private set
            {
                _status = value;

                StatusChanged?.Invoke(_status);

                if (_status >= SpawnStatus.Finalized || _status < SpawnStatus.None)
                    NotifyDoneListeners();
            }
        }

        /// <summary>
        /// Peer, who registered a started process for this task
        /// (for example, a game server)
        /// </summary>
        public IPeer RegisteredPeer { get; private set; }

        /// <summary>
        /// Who requested to spawn
        /// (most likely clients peer)
        /// Can be null
        /// </summary>
        public IPeer Requester { get; set; }

        public void OnProcessStarted()
        {
            if (!IsAborted && Status < SpawnStatus.WaitingForProcess)
            {
                Status = SpawnStatus.WaitingForProcess;
            }
        }

        public void OnProcessKilled()
        {
            Status = SpawnStatus.Killed;
        }

        public void OnRegistered(IPeer peerWhoRegistered)
        {
            RegisteredPeer = peerWhoRegistered;

            if (!IsAborted && Status < SpawnStatus.ProcessRegistered)
            {
                Status = SpawnStatus.ProcessRegistered;
            }
        }

        public void OnFinalized(SpawnFinalizationPacket finalizationPacket)
        {
            FinalizationPacket = finalizationPacket;
            if (!IsAborted && Status < SpawnStatus.Finalized)
            {
                Status = SpawnStatus.Finalized;
            }
        }

        public override string ToString()
        {
            return $"[SpawnTask: id - {SpawnId}]";
        }

        protected void NotifyDoneListeners()
        {
            foreach (var callback in _whenDoneCallbacks)
            {
                callback.Invoke(this);
            }

            _whenDoneCallbacks.Clear();
        }

        /// <summary>
        /// Callback will be called when spawn task is aborted or completed 
        /// (game server is opened)
        /// </summary>
        /// <param name="callback"></param>
        public SpawnTask WhenDone(Action<SpawnTask> callback)
        {
            _whenDoneCallbacks.Add(callback);
            return this;
        }

        public void Kill()
        {
            if (Status >= SpawnStatus.Finalized)
                return;

            
            Spawner.SendKillRequest(SpawnId, killed =>
            {
                Status = SpawnStatus.Killed;

                if (!killed)
                    Logs.Warn("Spawned Process might not have been killed");
            });
        }
        
    }
}