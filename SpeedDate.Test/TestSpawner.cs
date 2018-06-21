using System;
using System.Net;
using System.Threading;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Spawner;
using SpeedDate.Configuration;
using SpeedDate.Packets.Spawner;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Database.CockroachDb;
using Xunit;
using Xunit.Priority;

namespace SpeedDate.Test
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class TestSpawner
    {
        [Fact, Priority(100)]
        public void TestRegisterSpawner()
        {
            var are = new AutoResetEvent(false);

            var server = new SpeedDateServer();
            var client = new SpeedDateClient();
            client.Started += () =>
            {
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

            server.Started += () =>
            {
                client.Start(new DefaultConfigProvider(
                    new NetworkConfig(IPAddress.Loopback, 12345), //Connect to port 12345
                    new PluginsConfig("SpeedDate.ClientPlugins.Spawner*"))); //Load spawner-plugins only
            };

            server.Start(new DefaultConfigProvider(
                new NetworkConfig("0.0.0.0", 12345), //Listen in port 12345
                new PluginsConfig("SpeedDate.ServerPlugins.*"), //Load server-plugins only
                new IConfig[] {
                    new CockroachDbConfig
                    {
                        CheckConnectionOnStartup = false,
                        Port = 12346 //Set port to avoid exceptions
                    }
                })
            );

            are.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled

            server.Stop();
            server.Dispose();
        }
    }
}
