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
        public void IsConnected_ShouldBeFalse()
        {
            var client = new SpeedDateClient();
            client.IsConnected.ShouldBeFalse();
        }

        [Test]
        public void StopServer_ShouldStopClient()
        {
            var done = new AutoResetEvent(false);

            //We will stop the server later, so we create a separate one so other tests will not be affected
            var server = new SpeedDateServer();

            var client = new SpeedDateClient();

            server.Started += () =>
            {
                client.Start(new DefaultConfigProvider(
                    new NetworkConfig(IPAddress.Loopback, SetUp.Port+1), //Connect to port 
                    PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only
            };

            server.PeerConnected += peer => { done.Set(); };

            server.Start(new DefaultConfigProvider(
                new NetworkConfig("0.0.0.0", SetUp.Port+1), //Listen in port
                PluginsConfig.DefaultServerPlugins) //Load server-plugins only
            );

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();

            client.IsConnected.ShouldBeTrue("Client is connected");
            client.Stopped += () => done.Set();

            done.Reset();

            server.Stop();
            server.Dispose();

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();

            client.IsConnected.ShouldBeFalse("Client is no longer connected");
        }
    }
}
