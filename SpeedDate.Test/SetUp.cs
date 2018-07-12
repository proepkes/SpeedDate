using System.Collections.Generic;
using System.Net;
using Moq;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Configuration;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Database;
using SpeedDate.ServerPlugins.Database.Entities;
using SpeedDate.ServerPlugins.Lobbies;
using SpeedDate.ServerPlugins.Mail;

namespace SpeedDate.Test
{
    [SetUpFixture]
    public class SetUp
    {
        public const int Port = 12345;
        public const string GuestPrefix = "TestGuest-";
        public const string TestAccountPassword = "testPassword";
        
        public static readonly AccountData TestAccount = new AccountData
        {
            AccountId = 1,
            Email = "test@account.com",
            IsAdmin = false,
            IsEmailConfirmed = true,
            IsGuest = false,
            Password = Util.CreateHash(TestAccountPassword),
            Properties = new Dictionary<string, string>(),
            Token = "testToken",
            Username = "TestUser"
        };

        public static SpeedDateServer Server;
        public static Mock<ISmtpClient> SmtpClientMock = new Mock<ISmtpClient>();
        public static Mock<IDbAccess> DatabaseMock = new Mock<IDbAccess>();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DatabaseMock.Setup(db => db.CreateAccountObject()).Returns(new AccountData());
            
            DatabaseMock.Setup(db => db.GetAccount(TestAccount.Username)).Returns(TestAccount);
            DatabaseMock.Setup(db => db.GetAccount(It.IsNotIn(TestAccount.Username))).Returns(() => null);
            
            DatabaseMock.Setup(db => db.GetAccountByEmail(TestAccount.Email)).Returns(TestAccount);
            DatabaseMock.Setup(db => db.GetAccountByEmail(It.IsNotIn(TestAccount.Email))).Returns(() => null);
            
            DatabaseMock.Setup(db => db.GetAccountByToken(TestAccount.Token)).Returns(TestAccount);
            DatabaseMock.Setup(db => db.GetAccountByToken(It.IsNotIn(TestAccount.Token))).Returns(() => null);
            
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
            Server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("Deathmatch", Server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.Deathmatch));
            Server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("2v2v4", Server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.TwoVsTwoVsFour));
            Server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("3v3auto", Server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.ThreeVsThreeQueue));
            Server.GetPlugin<MailPlugin>().SetSmtpClient(SmtpClientMock.Object);
            Server.GetPlugin<DatabasePlugin>().SetDbAccess(DatabaseMock.Object);
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Server.Stop();
            Server.Dispose();
        }
    }
}
