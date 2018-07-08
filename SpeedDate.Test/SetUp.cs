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
        public static Mock<ISmtpClient> SmtpClientMock;
        public static Mock<IDbAccess> DatabaseMock;

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

            SmtpClientMock = new Mock<ISmtpClient>();
            Server.GetPlugin<MailPlugin>().SetSmtpClient(SmtpClientMock.Object);

            DatabaseMock = new Mock<IDbAccess>();
            DatabaseMock.Setup(access => access.CreateAccountObject()).Returns(new AccountData());
            DatabaseMock.Setup(access => access.GetAccount(TestAccount.Username)).Returns(TestAccount);
            DatabaseMock.Setup(access => access.GetAccountByEmail(TestAccount.Email)).Returns(TestAccount);
            DatabaseMock.Setup(access => access.GetAccountByToken(TestAccount.Token)).Returns(TestAccount);
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
