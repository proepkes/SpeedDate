using System;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.Configuration;
using SpeedDate.Server;


namespace SpeedDate.Test
{
    [TestFixture]
    public class TestAuth
    {
        [Test]
        public void TestLoginAsGuest()
        {
            var are = new AutoResetEvent(false);

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

                    are.Set();
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
        public void TestMultipleGuests()
        {
            var numberOfClients = 200;
            
            var isClientLoggedIn = new bool[numberOfClients];
            
            var doneEvent = new ManualResetEvent(false);
            
            for (var clientNumber = 0; clientNumber < numberOfClients; clientNumber++)
                ThreadPool.QueueUserWorkItem(state =>
                    {
                        Console.WriteLine($"Starting client {int.Parse(state.ToString())}");
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

                                    isClientLoggedIn[(int) state] = true;
                                    
                                    if (Interlocked.Decrement(ref numberOfClients) == 0)
                                        doneEvent.Set();
                                }, 
                                error =>
                                {
                                    Should.NotThrow(() => throw new Exception(error));
                                });
                        };

                        client.Start(new DefaultConfigProvider(
                            new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                            new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only
                        
                    }, clientNumber);
            
            doneEvent.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
        }
    }
}
