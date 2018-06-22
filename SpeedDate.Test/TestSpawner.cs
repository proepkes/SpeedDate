using System;
using System.Net;
using System.Threading;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Spawner;
using SpeedDate.Configuration;
using SpeedDate.Packets.Spawner;
using SpeedDate.Server;
using Xunit;
using Xunit.Abstractions;

namespace SpeedDate.Test
{
    public class TestSpawner
    {
        private readonly ITestOutputHelper _output;

        public TestSpawner(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory, InlineData(13500)]
        public void TestRegisterSpawner(int port)
        {
            var are = new AutoResetEvent(false);

            var server = new SpeedDateServer();
            var client = new SpeedDateClient();
            client.Started += () =>
            {
                _output.WriteLine("Client started");
                client.GetPlugin<SpawnerPlugin>().RegisterSpawner(new SpawnerOptions { Region = "EU" }, 
                    spawner =>
                    {
                        _output.WriteLine("Testing spawnerid...");
                        spawner.SpawnerId.ShouldBeGreaterThanOrEqualTo(0);

                        _output.WriteLine("Success");
                        are.Set();
                    },
                    error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
            };

            server.Started += () =>
            {
                _output.WriteLine("Server started");
                client.Start(new DefaultConfigProvider(
                    new NetworkConfig(IPAddress.Loopback, port), //Connect to port
                    new PluginsConfig("SpeedDate.ClientPlugins.Spawner*"))); //Load spawner-plugins only
            };



            _output.WriteLine("Starting server...");
            server.Start(new DefaultConfigProvider(
                new NetworkConfig("0.0.0.0", port), //Listen oo port 
                new PluginsConfig("SpeedDate.ServerPlugins*") //Load server-plugins only
            ));
            
            
            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue(); //Should be signaled
            
            server.Stop();
            server.Dispose();
        }
    }
}
