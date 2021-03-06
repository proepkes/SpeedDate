﻿using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.Configuration;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestAuth
    {
        [Test]
        public void LoginAsGuest_ShouldGenerateGuestUsername()
        {
            var done = new AutoResetEvent(false);

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.IsConnected.ShouldBeTrue();
                client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    client.IsConnected.ShouldBeTrue();
                    client.GetPlugin<AuthPlugin>().IsLoggedIn.ShouldBeTrue();

                    info.ShouldNotBeNull();
                    info.IsGuest.ShouldBeTrue();
                    info.IsAdmin.ShouldBeFalse();
                    info.Username.ShouldStartWith(SetUp.GuestPrefix);

                    done.Set();
                }, 
                error =>
                {
                    throw new Exception(error);
                });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
        }

        [Test]
        public void ShouldLogOut()
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

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled

            client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
            {
                done.Set();
            },
            error => throw new Exception(error));
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.GetPlugin<AuthPlugin>().LogOut();
            client.GetPlugin<AuthPlugin>().IsLoggedIn.ShouldBeFalse();

            //LogOut will raise "Started"-event
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.IsConnected.ShouldBeTrue();
        }

        [Test]
        public void SimultaneousGuestLogins_ShouldGenerateDistinctUsernames()
        {
            var numberOfClients = 20;
            IProducerConsumerCollection<string> generatedUsernames = new ConcurrentBag<string>();
            
            var done = new ManualResetEvent(false);
            
            for (var clientNumber = 0; clientNumber < numberOfClients; clientNumber++)
                ThreadPool.QueueUserWorkItem(state =>
                    {
                        var client = new SpeedDateClient();
                        client.Started += () =>
                        {
                            client.IsConnected.ShouldBeTrue();
                            client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                                {
                                    client.IsConnected.ShouldBeTrue();
                                    client.GetPlugin<AuthPlugin>().IsLoggedIn.ShouldBeTrue();

                                    info.ShouldNotBeNull();
                                    info.IsGuest.ShouldBeTrue();
                                    info.IsAdmin.ShouldBeFalse();
                                    info.Username.ShouldStartWith(SetUp.GuestPrefix);

                                    generatedUsernames.TryAdd(info.Username).ShouldBeTrue();

                                    //Set done after all clients are logged in
                                    if (Interlocked.Decrement(ref numberOfClients) == 0)
                                        done.Set();
                                }, 
                                error =>
                                {
                                    throw new Exception(error);
                                });
                        };

                        client.Start(new DefaultConfigProvider(
                            new NetworkConfig(SetUp.MasterServerIp, SetUp.MasterServerPort), //Connect to port
                            PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only
                        
                    }, clientNumber);
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
        }
        
        [Test]
        public void ShouldReLogInAsGuest()
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

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled

            client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    done.Set();
                },
                error =>
                {
                    throw new Exception(error);
                });
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.GetPlugin<AuthPlugin>().LogOut();

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    done.Set();
                },
                error => throw new Exception(error));
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        }
        
        [Test]
        public void ShouldLogin()
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

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled

            client.GetPlugin<AuthPlugin>().LogIn(SetUp.TestAccount.Username, SetUp.TestAccountPassword, info =>
                {
                    done.Set();
                },
                error => throw new Exception(error));
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.GetPlugin<AuthPlugin>().LogOut();
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        }
        
        [Test]
        public void ShouldReLogin()
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

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled by Started

            client.GetPlugin<AuthPlugin>().LogIn(SetUp.TestAccount.Username, SetUp.TestAccountPassword, info =>
                {
                    done.Set();
                },
                error => throw new Exception(error));
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled by Login
            
            client.GetPlugin<AuthPlugin>().LogOut();
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
            
            client.GetPlugin<AuthPlugin>().LogIn(SetUp.TestAccount.Username, SetUp.TestAccountPassword, info =>
                {
                    done.Set();
                },
                error => throw new Exception(error));
            
            done.WaitOne(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            
            client.GetPlugin<AuthPlugin>().LogOut();
            
            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        }
    }
}
