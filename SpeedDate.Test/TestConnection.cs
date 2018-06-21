using System;
using System.Net;
using System.Threading;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.Configuration;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Database.CockroachDb;
using Xunit;

namespace SpeedDate.Test
{
    public class TestConnection
    {
        [Theory]
        [InlineData(12345)]
        public void TestConnectionToLoopback(int port)
        {
            var are = new AutoResetEvent(false);

            var server = new SpeedDateServer();

            var client = new SpeedDateClient();

            server.Started += () =>
            {
                client.Start(new DefaultConfigProvider(
                    new NetworkConfig(IPAddress.Loopback, port), //Connect to port 
                    new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only
            };

            server.PeerConnected += peer => { are.Set(); };

            server.Start(new DefaultConfigProvider(
                new NetworkConfig("0.0.0.0", port), //Listen in port
                new PluginsConfig("SpeedDate.ServerPlugins.*") //Load server-plugins only
            ));

            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue();

            client.IsConnected.ShouldBeTrue("Client is connected");
            client.Stopped += () => are.Set();

            are.Reset();

            server.Stop();
            server.Dispose();

            are.WaitOne(TimeSpan.FromSeconds(5)).ShouldBeTrue();

            client.IsConnected.ShouldBeFalse("Client is no longer connected");
        }
    }
}
