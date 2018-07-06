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
        public void StopServer_ShouldAlsoStopClient()
        {
            var done = new AutoResetEvent(false);

            //A new server is created so other tests are not affected
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

            client.IsConnected.ShouldBeTrue();
            client.Stopped += () => done.Set();

            server.Stop();
            server.Dispose();

            done.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue();

            client.IsConnected.ShouldBeFalse();
        }

        [Test]
        public void Reconnect_ShouldReconnect()
        {
            var done = new AutoResetEvent(false);
            
            var client = new SpeedDateClient();

            client.Started += () =>
            {
                done.Set();
            };
            
            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port 
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only


            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.Reconnect(); 
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.IsConnected.ShouldBeTrue();
        }
    }
}
