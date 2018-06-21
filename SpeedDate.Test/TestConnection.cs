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
        [Fact]
        public void TestConnectionToLoopback()
        {
            var are = new AutoResetEvent(false);

            var server = new SpeedDateServer();

            var client = new SpeedDateClient();

            server.Started += () =>
            {
                client.Start(new DefaultConfigProvider(
                    new NetworkConfig(IPAddress.Loopback, 12345), //Connect to port 12345
                    new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only
            };

            server.PeerConnected += peer => { are.Set(); };

            server.Start(new DefaultConfigProvider(
                new NetworkConfig("0.0.0.0", 12345), //Listen in port 12345
                new PluginsConfig("SpeedDate.ServerPlugins.*"), //Load server-plugins only
                new IConfig[] { 
                    new CockroachDbConfig
                    {
                        CheckConnectionOnStartup = false,
                        Port = 12346 //Set port to avoid exceptions
                    }})
            );

            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue();

            client.IsConnected.ShouldBeTrue("Client is connected");
            client.Stopped += () => are.Set();

            are.Reset();

            server.Stop();
            server.Dispose();

            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue();

            client.IsConnected.ShouldBeFalse("Client is no longer connected");
        }
    }
}
