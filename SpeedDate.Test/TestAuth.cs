using System;
using System.Net;
using System.Threading;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Profile;
using SpeedDate.Configuration;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Database.CockroachDb;
using Xunit;
using Xunit.Priority;

namespace SpeedDate.Test
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class TestAuth
    {
        [Fact, Priority(100)]
        public void TestLoginAsGuest()
        {
            const string GUEST_PREFIX = "TestGuest-";

            var are = new AutoResetEvent(false);

            var server = new SpeedDateServer();
            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    client.IsConnected.ShouldBeTrue();

                    info.ShouldNotBeNull();
                    info.IsGuest.ShouldBeTrue();
                    info.IsAdmin.ShouldBeFalse();
                    info.Username.ShouldStartWith(GUEST_PREFIX);

                    are.Set();
                }, 
                error =>
                {
                    Should.NotThrow(() => throw new Exception(error));
                });
            };

            server.Started += () =>
            {
                client.Start(new DefaultConfigProvider(
                    new NetworkConfig(IPAddress.Loopback, 12345), //Connect to port 12345
                    new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only
            };
            
            server.Start(new DefaultConfigProvider(
                new NetworkConfig("0.0.0.0", 12345), //Listen in port 12345
                new PluginsConfig("SpeedDate.ServerPlugins.*"), //Load server-plugins only
                new IConfig[] {
                    new CockroachDbConfig
                    {
                        CheckConnectionOnStartup = false,
                        Port = 12346 //Set port to avoid exceptions
                    }, 
                    new AuthConfig
                    {
                        GuestPrefix = GUEST_PREFIX, 
                        EnableGuestLogin = true
                    }, 
                })
            );

            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue(); //Should be signaled
            
            server.Stop();
            server.Dispose();
        }
    }
}
