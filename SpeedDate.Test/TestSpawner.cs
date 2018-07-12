using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Moq;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
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
                    error => { throw new Exception(error); });
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
                    error =>
                    {
                        throw new Exception(error);
                    });
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
                    }, error => 
                    { 
                        throw new Exception(error); 
                    });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), 
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        }

        [Test]
        public void ShouldRegisterSpawnedProcess()
        {
            var spawnerDelegateMock = new Mock<ISpawnerRequestsDelegate>();
            spawnerDelegateMock.Setup(mock => mock.HandleSpawnRequest(It.IsAny<IIncommingMessage>(), It.IsAny<SpawnRequestPacket>())).Callback(
                (IIncommingMessage message, SpawnRequestPacket data) =>
                {
                    message.Respond(ResponseStatus.Success);
                    message.Peer.SendMessage((ushort)OpCodes.ProcessStarted, data.SpawnId);
                });

            var spawnerRegionName = TestContext.CurrentContext.Test.Name;
            var done = new AutoResetEvent(false);

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
                    error =>
                    {
                        throw new Exception(error);
                    });
            };

            spawner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultSpawnerPlugins, new IConfig[]{ new SpawnerConfig
                {
                    Region = spawnerRegionName
                }})); //Load spawner-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();

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
                            if (status == SpawnStatus.WaitingForProcess)
                            {
                                done.Set();
                            }
                        };
                    }, error =>
                    {
                        throw new Exception(error);
                    });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port),
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        }
    }
}
