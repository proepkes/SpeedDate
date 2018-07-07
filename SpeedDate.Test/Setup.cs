using System.Net;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Configuration;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Database;
using SpeedDate.ServerPlugins.Lobbies;

namespace SpeedDate.Test
{ 
    [SetUpFixture]
    public class SetUp
    {
        public const string GuestPrefix = "TestGuest-";
        public const int Port = 12345;

        public static SpeedDateServer Server;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Server = new SpeedDateServer();
            Server.Start(new DefaultConfigProvider(new NetworkConfig(IPAddress.Any, Port), PluginsConfig.DefaultServerPlugins, new IConfig[]
            {
                new DatabaseConfig
                {
                  CheckConnectionOnStartup  = false
                },
                new AuthConfig
                {
                    GuestPrefix = GuestPrefix,
                    EnableGuestLogin = true
                }
            }));
            
            Server.GetPlugin<LobbiesPlugin>().ShouldNotBeNull();
            Server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("2v2v4", Server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.TwoVsTwoVsFour));
            Server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("3v3auto", Server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.ThreeVsThreeQueue));
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Server.Stop();
            Server.Dispose();
        }
    }
}
