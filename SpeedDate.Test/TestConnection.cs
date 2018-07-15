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
            
            client.Started += () => done.Set();

            server.Started += () =>
            {
                client.Start(new DefaultConfigProvider(
                    new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort+1), //Connect to port 
                    PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only
            };

            server.Start(new DefaultConfigProvider(
                new NetworkConfig("0.0.0.0", SetUp.MasterServerPort+1), //Listen in port
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
        public void Reconnect_ShouldRaiseStopped()
        {
            var done = new AutoResetEvent(false);
            
            void SetAutoResetEvent()
            {
                done.Set();
            }
            
            var client = new SpeedDateClient();

            client.Started += SetAutoResetEvent;
            
            client.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port 
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only


            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.Started -= SetAutoResetEvent;
            
            client.Stopped += SetAutoResetEvent;
            client.Reconnect(); 
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        } 
        
        [Test]
        public void Reconnect_ShouldRaiseStartedTwice()
        {
            var done = new AutoResetEvent(false);
            
            var client = new SpeedDateClient();

            client.Started += () => done.Set();
            
            client.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port 
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only


            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.Reconnect(); 
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        }
        
        [Test]
        public void Client_ShouldRaiseDisconnect()
        {
            var done = new AutoResetEvent(false);
            
            var client = new SpeedDateClient();

            client.Started += () =>
            {
                done.Set();
            };
            
            client.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port 
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only


            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();

            client.Stopped += () => { done.Set(); };
            client.Stop(); 
            
            done.WaitOne(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            
            client.IsConnected.ShouldBeFalse();
        }
    }
}
