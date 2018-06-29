using System.Net;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Chat;
using SpeedDate.ClientPlugins.Peer.Lobby;
using SpeedDate.ClientPlugins.Peer.MatchMaker;
using SpeedDate.ClientPlugins.Peer.Profile;
using SpeedDate.ClientPlugins.Peer.Room;
using SpeedDate.ClientPlugins.Peer.Security;
using SpeedDate.ClientPlugins.Peer.SpawnRequest;
using SpeedDate.Configuration;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestKernel
    {
        [Test]
        public void TestGetPeerPlugins()
        {
            var client = new SpeedDateClient();
            client.Start(new DefaultConfigProvider( //Start loads the plugins
                new NetworkConfig(IPAddress.Loopback, SetUp.Port),
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            client.GetPlugin<AuthPlugin>().ShouldNotBeNull();
            client.GetPlugin<ChatPlugin>().ShouldNotBeNull();
            client.GetPlugin<LobbyPlugin>().ShouldNotBeNull();
            client.GetPlugin<MatchmakerPlugin>().ShouldNotBeNull();
            client.GetPlugin<ProfilePlugin>().ShouldNotBeNull();
            client.GetPlugin<RoomPlugin>().ShouldNotBeNull();
            client.GetPlugin<SecurityPlugin>().ShouldNotBeNull();
            client.GetPlugin<SpawnRequestPlugin>().ShouldNotBeNull();

            client.Stop();
            client.Dispose();
        }
    }
}
