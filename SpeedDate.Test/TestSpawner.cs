using System;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Spawner;
using SpeedDate.Configuration;
using SpeedDate.Packets.Spawner;
using SpeedDate.Server;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestSpawner
    {
        [Test]
        public void TestRegisterSpawner()
        {
            var are = new AutoResetEvent(false);

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.IsConnected.ShouldBeTrue();
                client.GetPlugin<SpawnerPlugin>().RegisterSpawner(new SpawnerOptions { Region = "EU" }, 
                    spawner =>
                    {
                        spawner.SpawnerId.ShouldBeGreaterThanOrEqualTo(0);

                        are.Set();
                    },
                    error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                new PluginsConfig("SpeedDate.ClientPlugins.Spawner*"))); //Load spawner-plugins only
            
            
            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue(); //Should be signaled
        }
    }
}
