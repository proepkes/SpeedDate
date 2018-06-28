using System;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Echo;
using SpeedDate.Configuration;
using SpeedDate.Server;


namespace SpeedDate.Test
{
    [TestFixture]
    public class TestConnection
    {
        [Test]
        //Tests connect/disconnect with a separated server
        public void TestConnectionToLoopback()
        {
            var are = new AutoResetEvent(false);
            
            var server = new SpeedDateServer();

            var client = new SpeedDateClient();

            server.Started += () =>
            {
                client.Start(new DefaultConfigProvider(
                    new NetworkConfig(IPAddress.Loopback, SetUp.Port+1), //Connect to port 
                    new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only
            };

            server.PeerConnected += peer => { are.Set(); };

            server.Start(new DefaultConfigProvider(
                new NetworkConfig("0.0.0.0", SetUp.Port+1), //Listen in port
                new PluginsConfig("SpeedDate.ServerPlugins.*") //Load server-plugins only
            ));

            are.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();

            client.IsConnected.ShouldBeTrue("Client is connected");
            client.Stopped += () => are.Set();

            are.Reset();

            server.Stop();
            server.Dispose();

            are.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();

            client.IsConnected.ShouldBeFalse("Client is no longer connected");
        }
    }
}
