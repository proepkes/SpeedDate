using System;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
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

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.IsConnected.ShouldBeTrue();
                client.GetPlugin<SpawnerPlugin>().RegisterSpawner(new SpawnerOptions { Region = "EU" }, 
                    spawner =>
                    {
                        spawner.SpawnerId.ShouldBeGreaterThanOrEqualTo(0);
                        done.Set();
                    },
                    error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultSpawnerPlugins)); //Load spawner-plugins only
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
        }
    }
}
