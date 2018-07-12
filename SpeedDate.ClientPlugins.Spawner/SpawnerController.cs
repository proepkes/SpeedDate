using System;
using System.Collections.Concurrent;
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
    public class SpawnerController
    {
        public delegate void SpawnRequestHandler(SpawnRequestPacket packet, IIncommingMessage message);
        public delegate bool KillSpawnedProcessHandler(int spawnId);

        public readonly IClient Client;

        public int SpawnerId { get; }

        private SpawnRequestHandler _spawnRequestHandler;
        private KillSpawnedProcessHandler _killRequestHandler;

        #region Default process spawn handling

        private readonly Logger _logger = LogManager.GetCurrentClassLogger(LogLevel.Warn);
        private readonly ConcurrentDictionary<int, Process> _processes = new ConcurrentDictionary<int, Process>();

        #endregion

        private readonly SpawnerPlugin _owner;

        public SpawnerController(SpawnerPlugin owner, int spawnerId, IClient client)
        {
            _owner = owner;

            Client = client;
            SpawnerId = spawnerId;

            _killRequestHandler = DefaultKillRequestHandler;
            _spawnRequestHandler = DefaultSpawnRequestHandler;
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
            _owner.NotifyProcessStarted(spawnId, processId, cmdArgs);
        }

        public void NotifyProcessKilled(int spawnId)
        {
            _owner.NotifyProcessKilled(spawnId);
        }

        public void UpdateProcessesCount(int count)
        {
            _owner.UpdateProcessesCount(SpawnerId, count);
        }

        public void HandleSpawnRequest(SpawnRequestPacket packet, IIncommingMessage message)
        {
            _spawnRequestHandler.Invoke(packet, message);
        }

        public bool HandleKillSpawnedProcessRequest(int spawnId)
        {
            return _killRequestHandler.Invoke(spawnId);
        }

        #region Default handlers

        public bool DefaultKillRequestHandler(int spawnId)
        {
            _logger.Debug("Default kill request handler started handling a request to kill a process");

            try
            {
                _processes.TryRemove(spawnId, out var process);
                process?.Kill();
            }
            catch (Exception e)
            {
                _logger.Error("Got error while killing a spawned process");
                _logger.Error(e);
                return false;
            }
            return true;
        }

        public void DefaultSpawnRequestHandler(SpawnRequestPacket packet, IIncommingMessage message)
        {
            _logger.Debug("Default spawn handler started handling a request to spawn process");

            var controller = _owner.GetController(packet.SpawnerId);

            if (controller == null)
            {
                message.Respond("Failed to spawn a process. Spawner controller not found", ResponseStatus.Failed);
                return;
            }

            var port = _owner.GetAvailablePort();
            
            // Machine Ip
            var machineIp = _owner.Config.MachineIp; 

            // Path to executable
            var path = _owner.Config.ExecutablePath;
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
            var spawnInBatchmode = _owner.Config.SpawnInBatchmode
                                   && !CommandLineArgs.DontSpawnInBatchmode;

            
            var startProcessInfo = new ProcessStartInfo(path)
            {
                CreateNoWindow = true,
                UseShellExecute = true,
                Arguments = " " +
                    (spawnInBatchmode ? "-batchmode -nographics " : "") +
                    (_owner.Config.AddWebGlFlag ? CommandLineArgs.Names.WebGl+" " : "") +
                    sceneNameArgument +
                            $"{CommandLineArgs.Names.MasterIp} {Client.Config.Network.Address} " +
                            $"{CommandLineArgs.Names.MasterPort} {Client.Config.Network.Port} " +
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

                                // Save the process
                            _processes[packet.SpawnId] = process;

                            var processId = process.Id;

                            // Notify server that we've successfully handled the request
                            //AppTimer.ExecuteOnMainThread(() =>
                            //{
                                message.Respond(ResponseStatus.Success);
                                controller.NotifyProcessStarted(packet.SpawnId, processId, startProcessInfo.Arguments);
                            //});

                            process.WaitForExit();
                        }
                    }
                    catch (Exception e)
                    {
                        if (!processStarted)
                            //AppTimer.ExecuteOnMainThread(() =>
                            //{
                                message.Respond(ResponseStatus.Failed);
                            //});

                        _logger.Error("An exception was thrown while starting a process. Make sure that you have set a correct build path. " +
                                     "We've tried to start a process at: '" + path+"'. You can change it at 'SpawnerBehaviour' component");
                        _logger.Error(e);
                    }
                    finally
                    {
                        // Remove the process
                        _processes.TryRemove(packet.SpawnId, out _);

                        //AppTimer.ExecuteOnMainThread(() =>
                        //{
                            // Release the port number
                            _owner.ReleasePort(port);

                            _logger.Debug("Notifying about killed process with spawn id: " + packet.SpawnerId);
                            controller.NotifyProcessKilled(packet.SpawnId);
                        //});
                    }

                }).Start();
            }
            catch (Exception e)
            {
                message.Respond(e.Message, ResponseStatus.Error);
                Logs.Error(e);
            }
        }

        #endregion
    }
}
