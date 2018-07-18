using System.Collections.Generic;
using System.Net;
using Moq;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Configuration;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Mail;
using SpeedDate.ServerPlugins.Lobbies;
using SpeedDate.ServerPlugins.Database;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Database.Entities;

namespace SpeedDate.Test
{
    [SetUpFixture]
    public class SetUp
    {
        public static readonly IPAddress MasterServerIp = IPAddress.Loopback;
        public const int MasterServerPort = 12345;
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

        public static readonly string TestLobbyOneVsOneXml = 
            @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                <Lobby DisplayName=""1 vs 1""
                      Autostart=""false""
                      EnableTeamSwitching=""true""
                      PlayAgainEnabled=""true""
                      EnableReadySystem=""true""
                      AllowJoiningWhenGameIsLive=""true""
                      EnableGameMasters=""true""
                      StartGameWhenAllReady=""false""
                      EnableManualStart=""true"" 
                      KeepAliveWithZeroPlayers=""false""
                      AllowPlayersChangeLobbyProperties=""true"">
                  <Teams>
                    <Team Name=""Team Blue"" MinPlayers=""1"" MaxPlayers=""1"" Color=""0000FF""/>
                    <Team Name=""Team Red"" MinPlayers=""1"" MaxPlayers=""1"" Color=""FF0000""/>
                  </Teams>
                  <Controls>
                    <Control Key=""speed"" Label=""Game Speed"">
                      <Controloption Value=""1x"" />
                      <Controloption IsDefault=""true"" Value=""2x"" />
                      <Controloption Value=""3x"" />
                    </Control>
                    <Control Key=""gravity"" Label=""Gravity"">
                      <Controloption Value=""On"" />
                      <Controloption Value=""Off"" />
                    </Control>
                  </Controls>
                </Lobby>";

        public static SpeedDateServer Server;
        public static readonly Mock<ISmtpClient> SmtpClientMock = new Mock<ISmtpClient>();
        public static readonly Mock<IDbAccess> DatabaseMock = new Mock<IDbAccess>();

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
            Server.Start(new DefaultConfigProvider(new NetworkConfig(IPAddress.Any, MasterServerPort), PluginsConfig.DefaultServerPlugins, new IConfig[]
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

            Server.GetPlugin<LobbiesPlugin>().Factories.Add("1v1", (plugin, properties, creator) => 
                new Lobby(plugin.GenerateLobbyId(), new[]
                {
                    new LobbyTeam("Team Blue") { MinPlayers = 1, MaxPlayers = 1}, 
                    new LobbyTeam("Team Red") { MinPlayers = 1, MaxPlayers = 1}
                }, plugin)
                {
                    Name = properties.ExtractLobbyName()
                });

            Server.GetPlugin<LobbiesPlugin>().Factories.Add("2v2v4", (plugin, properties, creator) =>
                new Lobby(plugin.GenerateLobbyId(), new[]
                {
                    new LobbyTeam("Team Blue") { MinPlayers = 1, MaxPlayers = 2},
                    new LobbyTeam("Team Red") { MinPlayers = 1, MaxPlayers = 2},
                    new LobbyTeam("Team Noobs") { MinPlayers = 1, MaxPlayers = 4}
                }, plugin)
                {
                    Name = properties.ExtractLobbyName()
                });
            Server.GetPlugin<LobbiesPlugin>().Factories.Add("3v3auto", (plugin, properties, creator) =>
                new Lobby(plugin.GenerateLobbyId(), new[] 
                {
                    new LobbyTeam("Team Blue") { MinPlayers = 1, MaxPlayers = 3},
                    new LobbyTeam("Team Red") { MinPlayers = 1, MaxPlayers = 3},
                }, plugin)
                {
                    Name = properties.ExtractLobbyName()
                });

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
