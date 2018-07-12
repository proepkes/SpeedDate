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
using SpeedDate.Packets.Common;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.Spawner
{
    public delegate void RegisterSpawnerCallback(int spawnerId);
    public delegate void SpawnRequestHandler(IIncommingMessage message);
    public delegate bool KillSpawnedProcessHandler(int spawnId);

    public class SpawnerPlugin : SpeedDateClientPlugin
    {
        [Inject] private ILogger _logger;
        [Inject] private SpawnerConfig _config;

        public const int PortsStartFrom = 10000;

        private readonly Queue<int> _freePorts;
        private int _lastPortTaken = -1;

        private SpawnRequestHandler _spawnRequestHandler;
        private KillSpawnedProcessHandler _killRequestHandler;
        private readonly ConcurrentDictionary<int, Process> _processes = new ConcurrentDictionary<int, Process>();

        public int SpawnerId { get; private set; }

        public SpawnerPlugin() 
        {
            _freePorts = new Queue<int>();

            _killRequestHandler = DefaultKillRequestHandler;
            _spawnRequestHandler = DefaultSpawnRequestHandler;
        }

        public override void Loaded()
        {
            Client.SetHandler((ushort)OpCodes.SpawnRequest, HandleSpawnRequest);
            Client.SetHandler((ushort)OpCodes.KillSpawnedProcess, HandleKillSpawnedProcessRequest);
        }

        /// <summary>
        /// Sends a request to master server, to register an existing spawner with given options
        /// </summary>
        public void Register(SpawnerOptions options, RegisterSpawnerCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            _logger.Info("Registering Spawner...");
            Client.SendMessage((ushort) OpCodes.RegisterSpawner, options, (status, response) =>
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

        /// <summary>
        /// Notifies master server, how many processes are running on this spawner
        /// </summary>
        public void UpdateProcessesCount(int spawnerId, int count)
        {
            var packet = new IntPairPacket
            {
                A = spawnerId,
                B = count
            };
            Client.SendMessage((ushort)OpCodes.UpdateSpawnerProcessesCount, packet);
        }

        public void SetSpawnRequestHandler(SpawnRequestHandler handler)
        {
            _spawnRequestHandler = handler;
        }

        public void SetKillRequestHandler(KillSpawnedProcessHandler handler)
        {
            _killRequestHandler = handler;
        }

        private int GetAvailablePort()
        {
            // Return a port from a list of available ports
            if (_freePorts.Count > 0)
                return _freePorts.Dequeue();

            if (_lastPortTaken < 0)
                _lastPortTaken = PortsStartFrom;

            return _lastPortTaken++;
        }

        private void ReleasePort(int port)
        {
            _freePorts.Enqueue(port);
        }
        
        private void NotifyProcessStarted(int spawnId, int processId, string cmdArgs)
        {
            if (!Client.IsConnected)
                return;

            Client.SendMessage((ushort)OpCodes.ProcessStarted, new SpawnedProcessStartedPacket
            {
                CmdArgs = cmdArgs,
                ProcessId = processId,
                SpawnId = spawnId
            });
        }

        private void NotifyProcessKilled(int spawnId)
        {
            if (!Client.IsConnected)
                return;

            Client.SendMessage((ushort)OpCodes.ProcessKilled, spawnId);
        }

        private bool HandleKillSpawnedProcessRequest(int spawnId)
        {
            return _killRequestHandler.Invoke(spawnId);
        }
        
        private void HandleSpawnRequest(IIncommingMessage message)
        {
            // Pass the request to handler
            _spawnRequestHandler.Invoke(message);
        }

        private void HandleKillSpawnedProcessRequest(IIncommingMessage message)
        {
            var data = message.Deserialize<KillSpawnedProcessPacket>();

            message.Respond(HandleKillSpawnedProcessRequest(data.SpawnId) ? ResponseStatus.Success : ResponseStatus.Failed);
        }

        private bool DefaultKillRequestHandler(int spawnId)
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

        private void DefaultSpawnRequestHandler(IIncommingMessage message)
        {
            var packet = message.Deserialize<SpawnRequestPacket>();
            if (packet == null)
            {
                message.Respond(ResponseStatus.Error);
                return;
            }
            _logger.Debug("Default spawn handler started handling a request to spawn process");

            var port = GetAvailablePort();

            // Machine Ip
            var machineIp = _config.MachineIp;

            // Path to executable
            var path = _config.ExecutablePath;
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
            var spawnInBatchmode = _config.SpawnInBatchmode
                                   && !CommandLineArgs.DontSpawnInBatchmode;


            var startProcessInfo = new ProcessStartInfo(path)
            {
                CreateNoWindow = true,
                UseShellExecute = true,
                Arguments = " " +
                    (spawnInBatchmode ? "-batchmode -nographics " : "") +
                    (_config.AddWebGlFlag ? CommandLineArgs.Names.WebGl + " " : "") +
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

                            // Save the process
                            _processes[packet.SpawnId] = process;

                            var processId = process.Id;

                            // Notify server that we've successfully handled the request
                            //AppTimer.ExecuteOnMainThread(() =>
                            //{
                            message.Respond(ResponseStatus.Success);
                            NotifyProcessStarted(packet.SpawnId, processId, startProcessInfo.Arguments);
                            //});

                            processStarted = true;
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
                                     "We've tried to start a process at: '" + path + "'. You can change it at 'SpawnerBehaviour' component");
                        _logger.Error(e);
                    }
                    finally
                    {
                        // Remove the process
                        _processes.TryRemove(packet.SpawnId, out _);

                        //AppTimer.ExecuteOnMainThread(() =>
                        //{
                        // Release the port number
                        ReleasePort(port);

                        _logger.Debug("Notifying about killed process with spawn id: " + packet.SpawnerId);
                        NotifyProcessKilled(packet.SpawnId);
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
    }
}
