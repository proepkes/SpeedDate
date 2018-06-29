using System.Net;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Configuration;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Lobbies;

namespace SpeedDate.Test
{ 
    [SetUpFixture]
    public class SetUp
    {
        public const string GuestPrefix = "TestGuest-";
        public const int Port = 12345;

        private SpeedDateServer _server;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _server = new SpeedDateServer();
            _server.Start(new DefaultConfigProvider(new NetworkConfig(IPAddress.Any, Port), PluginsConfig.DefaultServerPlugins, new []
            {
                new AuthConfig
                {
                    GuestPrefix = GuestPrefix,
                    EnableGuestLogin = true
                }
            }));
            
            _server.GetPlugin<LobbiesPlugin>().ShouldNotBeNull();
            _server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("2v2v4", _server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.TwoVsTwoVsFour));
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _server.Stop();
            _server.Dispose();
        }
    }
}
