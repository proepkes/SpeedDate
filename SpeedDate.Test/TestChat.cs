using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Chat;
using SpeedDate.Configuration;

namespace SpeedDate.Test
{

    [TestFixture]
    public class TestChat
    {
        [Test]
        public void TestGetEmptyChannels()
        {
            var are = new AutoResetEvent(false);

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.IsConnected.ShouldBeTrue();
                client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                    {
                        client.IsConnected.ShouldBeTrue();
                        client.GetPlugin<ChatPlugin>().GetJoinedChannels(channels =>
                        {
                            channels.ShouldNotBeNull();
                            channels.ShouldBeEmpty();
                            are.Set();
                        }, error => { Should.NotThrow(() => throw new Exception(error)); });
                    },
                    error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only

            are.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
        }

        [Test]
        public void TestJoinSingleChannel()
        {
            const string channelName = "General";
            
            var are = new AutoResetEvent(false);

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.IsConnected.ShouldBeTrue();
                client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                    {
                        client.IsConnected.ShouldBeTrue();
                        client.GetPlugin<ChatPlugin>().JoinChannel(channelName, () =>
                        {
                            client.IsConnected.ShouldBeTrue();
                            client.GetPlugin<ChatPlugin>().GetJoinedChannels(channels =>
                            {
                                channels.ShouldNotBeNull();
                                channels.Count.ShouldBe(1);
                                channels.ShouldContain(channelName);
                                
                                are.Set();
                            }, error => { Should.NotThrow(() => throw new Exception(error)); });
                        }, error => { Should.NotThrow(() => throw new Exception(error)); });
                    }, error => { Should.NotThrow(() => throw new Exception(error)); });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only

            are.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
        }
    }
}
