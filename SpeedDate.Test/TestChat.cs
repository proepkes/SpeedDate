using System;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Chat;
using SpeedDate.Configuration;
using SpeedDate.Packets.Chat;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestChat
    {
        [Test]
        public void GetJoinedChannels_ShouldBeEmpty()
        {
            var done = new AutoResetEvent(false);

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.IsConnected.ShouldBeTrue();
                client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                    {
                        client.GetPlugin<ChatPlugin>().GetJoinedChannels(channels =>
                        {
                            channels.ShouldNotBeNull();
                            channels.ShouldBeEmpty();
                            done.Set();
                        }, error => { throw new Exception(error); });
                    },
                    error =>
                    {
                        throw new Exception(error);
                    });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside GetJoinedChannels
        }

        [Test]
        public void GetJoinedChannels_ShouldContainJoinedChannel()
        {
            const string channelName = "General";
            
            var done = new AutoResetEvent(false);

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                    {
                        client.GetPlugin<ChatPlugin>().JoinChannel(channelName, () =>
                        {
                            client.GetPlugin<ChatPlugin>().GetJoinedChannels(channels =>
                            {
                                channels.ShouldNotBeNull();
                                channels.Count.ShouldBe(1);
                                channels.ShouldContain(channelName);
                                
                                done.Set();
                            }, error => { throw new Exception(error); });
                        }, error => { throw new Exception(error); });
                    }, error => { throw new Exception(error); });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside GetJoinedChannels
        }

        [Test]
        public void SendChannelMessage_ShouldBeReceived()
        {
            const string channelName = "MyChannel";
            const string message = "MyTest123";

            var usernameSlaveClient = string.Empty;

            var done = new AutoResetEvent(false);

            //Create Channel with MasterClient
            var masterClient = new SpeedDateClient();
            masterClient.Started += () =>
            {
                masterClient.GetPlugin<ChatPlugin>().MessageReceived += packet =>
                {
                    packet.Type.ShouldBe(ChatMessagePacket.ChannelMessage);
                    packet.Sender.ShouldBe(usernameSlaveClient);
                    packet.Receiver.ShouldBe(channelName);
                    packet.Message.ShouldBe(message);
                    done.Set();
                };

                masterClient.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    masterClient.GetPlugin<ChatPlugin>().JoinChannel(channelName, () =>
                    {
                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            masterClient.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside masterClient.GetJoinedChannels

            //Join Channel with slave-client
            var slaveClient = new SpeedDateClient();
            slaveClient.Started += () =>
            {
                slaveClient.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    usernameSlaveClient = info.Username;
                    slaveClient.GetPlugin<ChatPlugin>().JoinChannel(channelName, () =>
                    {
                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            slaveClient.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside slaveClient.Started

            usernameSlaveClient.ShouldNotBeNullOrEmpty();

            slaveClient.GetPlugin<ChatPlugin>().SendChannelMessage(channelName, message, () => { }, error => { throw new Exception(error); });

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside masterClient.MessageReceived
        }

        [Test]
        public void SendPrivateMessage_ShouldBeNotLoggedInError()
        {
            const string channelName = "MyChannel";
            const string message = "MyTest123";
            
            var done = new AutoResetEvent(false);

            //Join Channel with slave-client
            var slaveClient = new SpeedDateClient();
            slaveClient.Started += () =>
            {
                slaveClient.GetPlugin<ChatPlugin>().SendPrivateMessage(channelName, message, () => { },
                    error =>
                    {
                        error.ShouldNotBeNullOrEmpty("SendPrivateMessage without logging in");
                        done.Set();
                    });
            };

            slaveClient.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside slaveClient.SendPrivateMessage
        }

        [Test]
        public void SendPrivateMessage_ShouldBeReceived()
        {
            const string message = "MyTest123";

            var usernameMasterClient = string.Empty;
            var usernameSlaveClient = string.Empty;

            var done = new AutoResetEvent(false);

            //Create Channel with MasterClient
            var masterClient = new SpeedDateClient();
            masterClient.Started += () =>
            {
                masterClient.GetPlugin<ChatPlugin>().MessageReceived += packet =>
                {
                    packet.Type.ShouldBe(ChatMessagePacket.PrivateMessage);
                    packet.Receiver.ShouldBe(usernameMasterClient);
                    packet.Sender.ShouldBe(usernameSlaveClient);
                    packet.Message.ShouldBe(message);
                    done.Set();
                };

                masterClient.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    usernameMasterClient = info.Username;
                    done.Set();
                }, error => { throw new Exception(error); });
            };

            masterClient.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside masterClient.LogInAsGuest

            usernameMasterClient.ShouldNotBeNullOrEmpty();

            //Join Channel with slave-client
            var slaveClient = new SpeedDateClient();
            slaveClient.Started += () =>
            {
                slaveClient.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    usernameSlaveClient = info.Username;

                    slaveClient.IsConnected.ShouldBeTrue();
                    slaveClient.GetPlugin<ChatPlugin>().SendPrivateMessage(usernameMasterClient, message, () =>
                    {

                    }, error =>
                    {
                        throw new Exception(error);
                    });
                }, error => { throw new Exception(error); });
            };

            slaveClient.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside MessageReceived
        }
    }
}
