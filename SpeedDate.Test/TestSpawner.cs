using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Moq;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.GameServer;
using SpeedDate.ClientPlugins.Peer.SpawnRequest;
using SpeedDate.ClientPlugins.Spawner;
using SpeedDate.Configuration;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestSpawner
    {
        [Test]
        public void RegisterSpawner_ShouldGenerateSpawnerId()
        {
            var done = new AutoResetEvent(false);

            var spawner = new SpeedDateClient();
            spawner.Started += () =>
            {
                spawner.GetPlugin<SpawnerPlugin>().Register(
                    spawnerId =>
                    {
                        spawnerId.ShouldBeGreaterThanOrEqualTo(0);
                        done.Set();
                    },
                    error => throw new Exception(error));
            };

            spawner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), 
                PluginsConfig.DefaultSpawnerPlugins)); //Load spawner-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); 
        }
        
        [Test]
        public void RequestSpawnWithInvalidSpawnerSettings_ShouldAbort()
        {
            var spawnerRegionName = TestContext.CurrentContext.Test.Name;
            var done = new AutoResetEvent(false);

            //Register a spawner
            var spawner = new SpeedDateClient();
            spawner.Started += () =>
            {
                spawner.GetPlugin<SpawnerPlugin>().Register(
                    spawnerId =>
                    {
                        spawnerId.ShouldBeGreaterThanOrEqualTo(0);
                        done.Set();
                    },
                    error => throw new Exception(error));
            };

            spawner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), 
                PluginsConfig.DefaultSpawnerPlugins, new IConfig[]{ new SpawnerConfig
                {
                    Region = spawnerRegionName
                }})); //Load spawner-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Spawner is registered
            
            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.GetPlugin<SpawnRequestPlugin>().RequestSpawn(new Dictionary<string, string>(), spawnerRegionName,
                    controller => 
                    {
                        controller.ShouldNotBeNull();
                        controller.Status.ShouldBe(SpawnStatus.None);
                        controller.StatusChanged += status =>
                        {
                            if (status == SpawnStatus.Killed)
                            {
                                done.Set();
                            }
                        }; 
                    }, error => throw new Exception(error));
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), 
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        }

        [Test]
        public void ShouldRegisterSpawnedProcess()
        {
            var done = new AutoResetEvent(false);

            var spawnId = -1;
            var spawnCode = string.Empty;
            var spawnerRegionName = TestContext.CurrentContext.Test.Name;

            //Fakes spawning a process after receiving a SpawnRequest
            var spawnerDelegateMock = new Mock<ISpawnerRequestsDelegate>();
            spawnerDelegateMock.Setup(mock => mock.HandleSpawnRequest(
                    It.IsAny<IIncommingMessage>(), 
                    It.Is<SpawnRequestPacket>(packet => packet.SpawnId >= 0 && !string.IsNullOrEmpty(packet.SpawnCode)))).
                Callback((IIncommingMessage message, SpawnRequestPacket data) =>
                {
                    //By default, the spawn-data is passed via commandline-arguments
                    spawnId = data.SpawnId;
                    spawnCode = data.SpawnCode;

                    message.Respond(ResponseStatus.Success);
                    message.Peer.SendMessage((ushort)OpCodes.ProcessStarted, data.SpawnId);
                });

            //Register a spawner
            var spawner = new SpeedDateClient();
            spawner.Started += () =>
            {
                spawner.GetPlugin<SpawnerPlugin>().SetSpawnerRequestsDelegate(spawnerDelegateMock.Object);
                spawner.GetPlugin<SpawnerPlugin>().Register(
                    spawnerId =>
                    {
                        spawnerId.ShouldBeGreaterThanOrEqualTo(0);
                        done.Set();
                    },
                    error => throw new Exception(error));
            };

            spawner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultSpawnerPlugins, new IConfig[]{ new SpawnerConfig
                {
                    Region = spawnerRegionName
                }})); //Load spawner-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The spawner has been registered to master

            var client = new SpeedDateClient();
            client.Started += () => client.GetPlugin<SpawnRequestPlugin>().RequestSpawn(new Dictionary<string, string>(), spawnerRegionName,
                controller =>
                {
                    controller.ShouldNotBeNull();
                    controller.SpawnId.ShouldBeGreaterThanOrEqualTo(0);
                    controller.Status.ShouldBe(SpawnStatus.None);
                    controller.StatusChanged += status =>
                    {
                        switch (status)
                        {
                            case SpawnStatus.WaitingForProcess:
                            case SpawnStatus.ProcessRegistered:
                            case SpawnStatus.Finalized:
                                done.Set();
                                break;
                        }
                    };

                    spawnId = controller.SpawnId;
                }, error => throw new Exception(error));

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port),
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The SpawnRequest has been handled and is now waiting for the process to start

            //Start the gameserver - by default this is done by the spawner-handler
            var gameserver = new SpeedDateClient();
            gameserver.Started += () =>
            {
                //By default, the spawn-data is passed via commandline-arguments
                gameserver.GetPlugin<RoomsPlugin>().RegisterSpawnedProcess(spawnId, spawnCode, controller =>
                {
                   //StatusChanged => SpawnStatus.ProcessRegistered will signal done
                   controller.FinalizeTask(new Dictionary<string, string>(), () =>
                    {
                        //StatusChanged => SpawnStatus.Finalized will signal done
                    });
                }, error => throw new Exception(error));
            };

            gameserver.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port),
                PluginsConfig.DefaultGameServerPlugins)); //Load gameserver-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The Process has been registered

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The SpawnRequest has been finalized
        }

        [Test]
        public void ShouldPassFinalizationData()
        {
            var done = new AutoResetEvent(false);

            var spawnId = -1;
            var spawnCode = string.Empty;
            var spawnerRegionName = TestContext.CurrentContext.Test.Name;
            var testData = new KeyValuePair<string, string>("Hello", "World");

            //Fakes spawning a process after receiving a SpawnRequest
            var spawnerDelegateMock = new Mock<ISpawnerRequestsDelegate>();
            spawnerDelegateMock.Setup(mock => mock.HandleSpawnRequest(
                    It.IsAny<IIncommingMessage>(),
                    It.Is<SpawnRequestPacket>(packet => packet.SpawnId >= 0 && !string.IsNullOrEmpty(packet.SpawnCode)))).
                Callback((IIncommingMessage message, SpawnRequestPacket data) =>
                {
                    //By default, the spawn-data is passed via commandline-arguments
                    spawnId = data.SpawnId;
                    spawnCode = data.SpawnCode;

                    message.Respond(ResponseStatus.Success);
                    message.Peer.SendMessage((ushort)OpCodes.ProcessStarted, data.SpawnId);
                });

            //Register a spawner
            var spawner = new SpeedDateClient();
            spawner.Started += () =>
            {
                spawner.GetPlugin<SpawnerPlugin>().SetSpawnerRequestsDelegate(spawnerDelegateMock.Object);
                spawner.GetPlugin<SpawnerPlugin>().Register(
                    spawnerId =>
                    {
                        spawnerId.ShouldBeGreaterThanOrEqualTo(0);
                        done.Set();
                    },
                    error => throw new Exception(error));
            };

            spawner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultSpawnerPlugins, new IConfig[]{ new SpawnerConfig
                {
                    Region = spawnerRegionName
                }})); //Load spawner-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The spawner has been registered to master

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.GetPlugin<SpawnRequestPlugin>().RequestSpawn(new Dictionary<string, string>(), spawnerRegionName,
                    controller =>
                    {
                        controller.ShouldNotBeNull();
                        controller.SpawnId.ShouldBeGreaterThanOrEqualTo(0);
                        controller.Status.ShouldBe(SpawnStatus.None);
                        controller.StatusChanged += status =>
                        {
                            switch (status)
                            {
                                case SpawnStatus.WaitingForProcess:
                                case SpawnStatus.ProcessRegistered:
                                case SpawnStatus.Finalized:
                                    done.Set();
                                    break;
                            }
                        };

                        spawnId = controller.SpawnId;
                    }, error => throw new Exception(error));
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port),
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The SpawnRequest has been handled and is now waiting for the process to start

            //Start the gameserver - by default this is done by the spawner-handler
            var gameserver = new SpeedDateClient();
            gameserver.Started += () =>
            {
                //By default, the spawn-data is passed via commandline-arguments
                gameserver.GetPlugin<RoomsPlugin>().RegisterSpawnedProcess(spawnId, spawnCode, controller =>
                {
                    //StatusChanged => SpawnStatus.ProcessRegistered will signal done
                    controller.FinalizeTask(new Dictionary<string, string> {  {testData.Key, testData.Value} }, () =>
                    {
                        //StatusChanged => SpawnStatus.Finalized will signal done
                    });
                }, error => throw new Exception(error));
            };

            gameserver.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port),
                PluginsConfig.DefaultGameServerPlugins)); //Load gameserver-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The Process has been registered

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The SpawnRequest has been finalized

            client.GetPlugin<SpawnRequestPlugin>().GetRequestController(spawnId).ShouldNotBeNull();
            client.GetPlugin<SpawnRequestPlugin>().GetRequestController(spawnId).GetFinalizationData(data =>
            {
                data.ShouldNotBeNull();
                data.ShouldContainKeyAndValue(testData.Key, testData.Value);
                done.Set();
            },
            error => throw new Exception(error));

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //FinalizationData was correct
        }

        [Test]
        public void EvilClientRegisterSpawnedProcess_ShouldError()
        {
            var done = new AutoResetEvent(false);

            //Start the gameserver - by default this is done by the spawner-handler
            var evilClient = new SpeedDateClient();
            evilClient.Started += () =>
            {
                //By default, the spawn-data is passed via commandline-arguments
                evilClient.GetPlugin<RoomsPlugin>().RegisterSpawnedProcess(Util.CreateRandomInt(0, 100), Util.CreateRandomString(10), controller => { }, 
                error =>
                {
                    done.Set();
                });
            };

            evilClient.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port),
                PluginsConfig.DefaultGameServerPlugins)); //Load gameserver-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Error occured
        }

        [Test]
        public void EvilClientWithCorrectSpawnIdRegisterSpawnedProcess_ShouldNotBeAuthorized()
        {
            var done = new AutoResetEvent(false);

            var spawnId = -1;
            var spawnerRegionName = TestContext.CurrentContext.Test.Name;

            //Fakes spawning a process after receiving a SpawnRequest
            var spawnerDelegateMock = new Mock<ISpawnerRequestsDelegate>();
            spawnerDelegateMock.Setup(mock => mock.HandleSpawnRequest(
                    It.IsAny<IIncommingMessage>(),
                    It.Is<SpawnRequestPacket>(packet => packet.SpawnId >= 0 && !string.IsNullOrEmpty(packet.SpawnCode)))).
                Callback((IIncommingMessage message, SpawnRequestPacket data) =>
                {
                    //By default, the spawn-data is passed via commandline-arguments
                    spawnId = data.SpawnId;
                    message.Respond(ResponseStatus.Success);
                    message.Peer.SendMessage((ushort)OpCodes.ProcessStarted, data.SpawnId);
                });

            //Register a spawner
            var spawner = new SpeedDateClient();
            spawner.Started += () =>
            {
                spawner.GetPlugin<SpawnerPlugin>().SetSpawnerRequestsDelegate(spawnerDelegateMock.Object);
                spawner.GetPlugin<SpawnerPlugin>().Register(
                    spawnerId =>
                    {
                        spawnerId.ShouldBeGreaterThanOrEqualTo(0);
                        done.Set();
                    },
                    error => throw new Exception(error));
            };

            spawner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultSpawnerPlugins, new IConfig[]{ new SpawnerConfig
                {
                    Region = spawnerRegionName
                }})); //Load spawner-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The spawner has been registered to master

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.GetPlugin<SpawnRequestPlugin>().RequestSpawn(new Dictionary<string, string>(), spawnerRegionName,
                    controller =>
                    {
                        controller.StatusChanged += status =>
                        {
                            switch (status)
                            {
                                case SpawnStatus.WaitingForProcess:
                                case SpawnStatus.ProcessRegistered:
                                    done.Set();
                                    break;
                            }
                        };

                        spawnId = controller.SpawnId;
                    }, error => throw new Exception(error));
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port),
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //The SpawnRequest has been handled and is now waiting for the process to start

            var evilClient = new SpeedDateClient();
            evilClient.Started += () =>
            {
                evilClient.GetPlugin<RoomsPlugin>().FinalizeSpawnedProcess(spawnId, () => {}, error => done.Set()); //Not authorized without registering with correct SpawnCode first
            };

            evilClient.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port),
                PluginsConfig.DefaultGameServerPlugins)); //Load gameserver-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Finalize returned an error
        }
    }
}