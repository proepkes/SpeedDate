using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SpeedDate.Configuration;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.Spawner
{
    public interface ISpawnerRequestsDelegate
    {
        void HandleSpawnRequest(IIncommingMessage message, SpawnRequestPacket data);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Kills a previously requested spawn </summary>
        ///
        /// <param name="spawnId">  Identifier for the spawn retrieved from the Spawnrequestpacket.SpawnId-field. </param>
        ///
        /// <returns>   True if the spawn with <paramref name="spawnId "/> was killed, false if it fails. </returns>
        ///-------------------------------------------------------------------------------------------------
        bool HandleKillRequest(int spawnId);
    }

    class ProcessSpawnerRequestHandler : ISpawnerRequestsDelegate
    {
        private readonly IClient _client;
        private readonly SpawnerConfig _spawnerConfig;
        private readonly ConcurrentDictionary<int, Process> _processes = new ConcurrentDictionary<int, Process>();

        public const int PortsStartFrom = 10000;

        private int _lastPortTaken = -1;
        private readonly ConcurrentQueue<int> _freePorts = new ConcurrentQueue<int>();

        public ProcessSpawnerRequestHandler(IClient client, SpawnerConfig spawnerConfig)
        {
            _client = client;
            _spawnerConfig = spawnerConfig;
        }

        public void HandleSpawnRequest(IIncommingMessage message, SpawnRequestPacket data)
        {
            var port = GetAvailablePort();

            // Machine Ip
            var machineIp = _spawnerConfig.MachineIp;

            // Path to executable
            var path = _spawnerConfig.ExecutablePath;
            if (string.IsNullOrEmpty(path))
            {
                path = File.Exists(Environment.GetCommandLineArgs()[0])
                    ? Environment.GetCommandLineArgs()[0]
                    : Process.GetCurrentProcess().MainModule.FileName;
            }

            // In case a path is provided with the request
            if (data.Properties.ContainsKey(OptionKeys.ExecutablePath))
                path = data.Properties[OptionKeys.ExecutablePath];

            // Get the scene name
            var sceneNameArgument = data.Properties.ContainsKey(OptionKeys.SceneName)
                ? $"{CommandLineArgs.Names.LoadScene} {data.Properties[OptionKeys.SceneName]} "
                : "";

            if (!string.IsNullOrEmpty(data.OverrideExePath))
            {
                path = data.OverrideExePath;
            }

            // If spawn in batchmode was set and `DontSpawnInBatchmode` arg is not provided
            var spawnInBatchmode = _spawnerConfig.SpawnInBatchmode
                                   && !CommandLineArgs.DontSpawnInBatchmode;


            var startProcessInfo = new ProcessStartInfo(path)
            {
                CreateNoWindow = true,
                UseShellExecute = true,
                Arguments = " " +
                    (spawnInBatchmode ? "-batchmode -nographics " : "") +
                    (_spawnerConfig.AddWebGlFlag ? CommandLineArgs.Names.WebGl + " " : "") +
                    sceneNameArgument +
                            $"{CommandLineArgs.Names.MasterIp} {_client.Config.Network.Address} " +
                            $"{CommandLineArgs.Names.MasterPort} {_client.Config.Network.Port} " +
                            $"{CommandLineArgs.Names.SpawnId} {data.SpawnId} " +
                            $"{CommandLineArgs.Names.AssignedPort} {port} " +
                            $"{CommandLineArgs.Names.MachineIp} {machineIp} " +
                            $"{CommandLineArgs.Names.SpawnCode} \"{data.SpawnCode}\" " +
                    data.CustomArgs
            };

            var processStarted = false;

            try
            {
                new Thread(() =>
                {
                    try
                    {
                        using (var process = Process.Start(startProcessInfo))
                        {
                            // Save the process
                            _processes[data.SpawnId] = process;

                            var processId = process.Id;

                            // Notify server that we've successfully handled the request
                            //AppTimer.ExecuteOnMainThread(() =>
                            //{
                            message.Respond(ResponseStatus.Success);
                            NotifyProcessStarted(data.SpawnId, processId, startProcessInfo.Arguments);
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
                    }
                    finally
                    {
                        // Remove the process
                        _processes.TryRemove(data.SpawnId, out _);

                        //AppTimer.ExecuteOnMainThread(() =>
                        //{
                        // Release the port number
                        ReleasePort(port);

                        NotifyProcessKilled(data.SpawnId);
                        //});
                    }

                }).Start();
            }
            catch (Exception e)
            {
                message.Respond(e.Message, ResponseStatus.Error);
            }
        }

        public bool HandleKillRequest(int spawnId)
        {
            try
            {
                _processes.TryRemove(spawnId, out var process);
                process?.Kill();
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        private int GetAvailablePort()
        {
            // Return a port from a list of available ports
            if (_freePorts.Count > 0 && _freePorts.TryDequeue(out var freeport))
                return freeport;

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
            if (!_client.IsConnected)
                return;

            _client.SendMessage((ushort)OpCodes.ProcessStarted, new SpawnedProcessStartedPacket
            {
                CmdArgs = cmdArgs,
                ProcessId = processId,
                SpawnId = spawnId
            });
        }

        private void NotifyProcessKilled(int spawnId)
        {
            if (!_client.IsConnected)
                return;

            _client.SendMessage((ushort)OpCodes.ProcessKilled, spawnId);
        }

    }
}