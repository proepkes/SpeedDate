using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.Spawner
{
    public partial class SpawnerController
    {
        public delegate void SpawnRequestHandler(SpawnRequestPacket packet, IIncommingMessage message);
        public delegate void KillSpawnedProcessHandler(int spawnId);

        public readonly IClientSocket Connection;

        public int SpawnerId { get; set; }
        public SpawnerOptions Options { get; private set; }

        private SpawnRequestHandler _spawnRequestHandler;
        private KillSpawnedProcessHandler _killRequestHandler;
        
        /// <summary>
        /// Settings, which are used by the default spawn handler
        /// </summary>
        public SpawnerConfig SpawnerSettings { get; private set; }

        #region Default process spawn handling

        private readonly Logger _logger = LogManager.GetCurrentClassLogger(LogLevel.Warn);
        private readonly object _processLock = new object();
        private readonly Dictionary<int, Process> _processes = new Dictionary<int, Process>();

        #endregion

        private readonly SpawnerPlugin _spawners;

        public SpawnerController(SpawnerPlugin owner, int spawnerId, IClientSocket connection, SpawnerOptions options)
        {
            _spawners = owner;

            Connection = connection;
            SpawnerId = spawnerId;
            Options = options;

            SpawnerSettings = SpeedDateConfig.Get<SpawnerConfig>();

            // Add handlers
            connection.SetHandler((ushort) OpCodes.SpawnRequest, HandleSpawnRequest);
            connection.SetHandler((ushort) OpCodes.KillSpawnedProcess, HandleKillSpawnedProcessRequest);
        }

        public void SetSpawnRequestHandler(SpawnRequestHandler handler)
        {
            _spawnRequestHandler = handler;
        }

        public void SetKillRequestHandler(KillSpawnedProcessHandler handler)
        {
            _killRequestHandler = handler;
        }

        public void NotifyProcessStarted(int spawnId, int processId, string cmdArgs)
        {
            _spawners.NotifyProcessStarted(spawnId, processId, cmdArgs);
        }

        public void NotifyProcessKilled(int spawnId)
        {
            _spawners.NotifyProcessKilled(spawnId);
        }

        public void UpdateProcessesCount(int count)
        {
            _spawners.UpdateProcessesCount(SpawnerId, count);
        }

        private void HandleSpawnRequest(SpawnRequestPacket packet, IIncommingMessage message)
        {
            if (_spawnRequestHandler == null)
            {
                DefaultSpawnRequestHandler(packet, message);
                return;
            }

            _spawnRequestHandler.Invoke(packet, message);
        }

        private void HandleKillSpawnedProcessRequest(int spawnId)
        {
            if (_killRequestHandler == null)
            {
                DefaultKillRequestHandler(spawnId);
                return;
            }

            _killRequestHandler.Invoke(spawnId);
        }

        private void HandleSpawnRequest(IIncommingMessage message)
        {
            var data = message.Deserialize(new SpawnRequestPacket());

            var controller = _spawners.GetController(data.SpawnerId);

            if (controller == null)
            {
                if (message.IsExpectingResponse) 
                    message.Respond("Couldn't find a spawn controller", ResponseStatus.NotHandled);
                return;
            }

            // Pass the request to handler
            controller.HandleSpawnRequest(data, message);
        }

        private void HandleKillSpawnedProcessRequest(IIncommingMessage message)
        {
            var data = message.Deserialize(new KillSpawnedProcessPacket());

            var controller = _spawners.GetController(data.SpawnerId);

            if (controller == null)
            {
                if (message.IsExpectingResponse)
                    message.Respond("Couldn't find a spawn controller", ResponseStatus.NotHandled);
                return;
            }

            controller.HandleKillSpawnedProcessRequest(data.SpawnId);
        }

        #region Default handlers

        public void DefaultKillRequestHandler(int spawnId)
        {
            _logger.Debug("Default kill request handler started handling a request to kill a process");

            try
            {
                Process process;

                lock (_processLock)
                {
                    _processes.TryGetValue(spawnId, out process);
                    _processes.Remove(spawnId);
                }

                process?.Kill();
            }
            catch (Exception e)
            {
                _logger.Error("Got error while killing a spawned process");
                _logger.Error(e);
            }
        }

        public void DefaultSpawnRequestHandler(SpawnRequestPacket packet, IIncommingMessage message)
        {
            _logger.Debug("Default spawn handler started handling a request to spawn process");

            var controller = _spawners.GetController(packet.SpawnerId);

            if (controller == null)
            {
                message.Respond("Failed to spawn a process. Spawner controller not found", ResponseStatus.Failed);
                return;
            }

            var port = _spawners.GetAvailablePort();
            
            // Machine Ip
            var machineIp = controller.SpawnerSettings.MachineIp; 

            // Path to executable
            var path = controller.SpawnerSettings.ExecutablePath;
            if (string.IsNullOrEmpty(path))
            {
                path = File.Exists(Environment.GetCommandLineArgs()[0]) 
                    ? Environment.GetCommandLineArgs()[0] 
                    : Process.GetCurrentProcess().MainModule.FileName;
            }

            // In case a path is provided with the request
            if (packet.Properties.ContainsKey(OptionKeys.ExecutablePath))
                path = packet.Properties[OptionKeys.ExecutablePath];

            // Get the scene name
            var sceneNameArgument = packet.Properties.ContainsKey(OptionKeys.SceneName)
                ? $"{CommandLineArgs.Names.LoadScene} {packet.Properties[OptionKeys.SceneName]} "
                : "";

            if (!string.IsNullOrEmpty(packet.OverrideExePath))
            {
                path = packet.OverrideExePath;
            }

            // If spawn in batchmode was set and `DontSpawnInBatchmode` arg is not provided
            var spawnInBatchmode = controller.SpawnerSettings.SpawnInBatchmode
                                   && !CommandLineArgs.DontSpawnInBatchmode;

            var startProcessInfo = new ProcessStartInfo(path)
            {
                CreateNoWindow = true,
                UseShellExecute = true,
                Arguments = " " +
                    (spawnInBatchmode ? "-batchmode -nographics " : "") +
                    (controller.SpawnerSettings.AddWebGlFlag ? CommandLineArgs.Names.WebGl+" " : "") +
                    sceneNameArgument +
                            $"{CommandLineArgs.Names.MasterIp} {controller.Connection.ConnectionIp} " +
                            $"{CommandLineArgs.Names.MasterPort} {controller.Connection.ConnectionPort} " +
                            $"{CommandLineArgs.Names.SpawnId} {packet.SpawnId} " +
                            $"{CommandLineArgs.Names.AssignedPort} {port} " +
                            $"{CommandLineArgs.Names.MachineIp} {machineIp} " +
                            $"{CommandLineArgs.Names.SpawnCode} \"{packet.SpawnCode}\" " +
                    packet.CustomArgs
            };

            _logger.Debug("Starting process with args: " + startProcessInfo.Arguments);

            var processStarted = false;

            try
            {
                new Thread(() =>
                {
                    try
                    {
                        _logger.Debug("New thread started");

                        using (var process = Process.Start(startProcessInfo))
                        {
                            _logger.Debug("Process started. Spawn Id: " + packet.SpawnId + ", pid: " + process.Id);
                            processStarted = true;

                            lock (_processLock)
                            {
                                // Save the process
                                _processes[packet.SpawnId] = process;
                            }

                            var processId = process.Id;

                            // Notify server that we've successfully handled the request
                            AppTimer.ExecuteOnMainThread(() =>
                            {
                                message.Respond(ResponseStatus.Success);
                                controller.NotifyProcessStarted(packet.SpawnId, processId, startProcessInfo.Arguments);
                            });

                            process.WaitForExit();
                        }
                    }
                    catch (Exception e)
                    {
                        if (!processStarted)
                            AppTimer.ExecuteOnMainThread(() => { message.Respond(ResponseStatus.Failed); });

                        _logger.Error("An exception was thrown while starting a process. Make sure that you have set a correct build path. " +
                                     "We've tried to start a process at: '" + path+"'. You can change it at 'SpawnerBehaviour' component");
                        _logger.Error(e);
                    }
                    finally
                    {
                        lock (_processLock)
                        {
                            // Remove the process
                            _processes.Remove(packet.SpawnId);
                        }

                        AppTimer.ExecuteOnMainThread(() =>
                        {
                            // Release the port number
                            _spawners.ReleasePort(port);

                            _logger.Debug("Notifying about killed process with spawn id: " + packet.SpawnerId);
                            controller.NotifyProcessKilled(packet.SpawnId);
                        });
                    }

                }).Start();
            }
            catch (Exception e)
            {
                message.Respond(e.Message, ResponseStatus.Error);
                Logs.Error(e);
            }
        }

        public void KillProcessesSpawnedWithDefaultHandler()
        {
            var list = new List<Process>();
            lock (_processLock)
            {
                foreach (var process in _processes.Values)
                {
                    list.Add(process);
                }
            }

            foreach (var process in list)
            {
                process.Kill();
            }
        }

        #endregion
    }
}