using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.SpawnRequest;
using SpeedDate.ClientPlugins.Spawner;
using SpeedDate.Configuration;
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
                spawner.GetPlugin<SpawnerPlugin>().RegisterSpawner(new SpawnerOptions {Region = "EU"},
                    controller =>
                    {
                        controller.SpawnerId.ShouldBeGreaterThanOrEqualTo(0);
                        done.Set();
                    },
                    error => { throw new Exception(error); });
            };

            spawner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultSpawnerPlugins)); //Load spawner-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
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
                spawner.GetPlugin<SpawnerPlugin>().RegisterSpawner(new SpawnerOptions {Region = spawnerRegionName},
                    controller =>
                    {
                        controller.SpawnerId.ShouldBeGreaterThanOrEqualTo(0);
                        done.Set();
                    },
                    error => { throw new Exception(error); });
            };

            spawner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultSpawnerPlugins)); //Load spawner-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Spawner is registered

            var expectedSpawnerStatus = SpawnStatus.Aborting;
            
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
                            if (status == expectedSpawnerStatus)
                            {
                                //First Status: Aborting
                                if (status == SpawnStatus.Aborting)
                                {
                                    expectedSpawnerStatus = SpawnStatus.Aborted;
                                }
                                else
                                {
                                    //LastStatus: Aborted
                                    done.Set();
                                }
                            }
                        }; 
                    }, error => 
                    { 
                        throw new Exception(error); 
                    });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load spawner-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
        }
    }
}
