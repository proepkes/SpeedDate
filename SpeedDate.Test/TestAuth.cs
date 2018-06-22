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

namespace SpeedDate.Test
{
    public class TestAuth
    {
        [Theory]
        [InlineData(12000)]
        public void TestLoginAsGuest(int port)
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
                    new NetworkConfig(IPAddress.Loopback, port), //Connect to port
                    new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only
            };
            
            server.Start(new DefaultConfigProvider(
                new NetworkConfig("0.0.0.0", port), //Listen on port
                new PluginsConfig("SpeedDate.ServerPlugins.*"), //Load server-plugins only
                new IConfig[] {
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
