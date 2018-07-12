using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Moq;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Lobby;
using SpeedDate.ClientPlugins.Peer.MatchMaker;
using SpeedDate.Configuration;
using SpeedDate.Packets.Lobbies;
using SpeedDate.Packets.Matchmaking;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestLobby
    {
        [Test]
        public void ShouldCreateDeathmatchLobby()
        {
            var lobbyName = TestContext.CurrentContext.Test.Name;
            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("Deathmatch", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobby.Data.GameMaster.ShouldBe(info.Username);
                        lobby.Members.ShouldContainKey(info.Username);

                        lobby.LobbyName.ShouldBe(lobbyName);
                        lobby.State.ShouldBe(LobbyState.Preparations);
                        lobby.Id.ShouldBeGreaterThanOrEqualTo(0);
                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created
        }

        [Test]
        public void ShouldNotGetLobbyRoomAccessWithoutJoiningLobbyFirst()
        {
            var lobbyName = TestContext.CurrentContext.Test.Name;
            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().GetLobbyRoomAccess(new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        throw new Exception("Got Lobby access without joining a lobby");
                    }, error =>
                    {
                        error.ShouldNotBeNullOrEmpty();
                        done.Set();
                    });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created
        }

        [Test]
        public void CreateAndJoinLobby_ShouldBeCreated()
        {
            var lobbyName = TestContext.CurrentContext.Test.Name;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("2v2v4", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobby.Data.GameMaster.ShouldBe(info.Username);
                        lobby.Members.ShouldContainKey(info.Username);

                        lobby.LobbyName.ShouldBe(lobbyName);
                        lobby.State.ShouldBe(LobbyState.Preparations);
                        lobby.Id.ShouldBeGreaterThanOrEqualTo(0);

                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created
        }

        [Test]
        public void CreateAndJoinAutoLobby_ShouldBeCreated()
        {
            var lobbyId = -1;
            var lobbyName = TestContext.CurrentContext.Test.Name;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("3v3auto", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobby.Data.GameMaster.ShouldBeEmpty();
                        lobby.Members.ShouldContainKey(info.Username);

                        lobby.LobbyName.ShouldBe(lobbyName);
                        lobby.Id.ShouldBeGreaterThanOrEqualTo(0);
                        lobby.State.ShouldBe(LobbyState.Preparations);

                        lobbyId = lobby.Id;

                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created
        }

        [Test]
        public void JoinLobby_ShouldIncreaseMembersCount()
        {
            var lobbyName = TestContext.CurrentContext.Test.Name;
            var lobbyId = -1;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("2v2v4", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobbyId = lobby.Id;
                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created

            var lobbyJoiner = new SpeedDateClient();
            lobbyJoiner.Started += () =>
            {
                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobbyId, joinedLobby =>
                    {
                        joinedLobby.Id.ShouldBe(lobbyId);
                        joinedLobby.Members.Count.ShouldBe(2);
                        joinedLobby.Members.ShouldContainKey(info.Username);

                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined
        }

        [Test]
        public void JoinLobby_ShouldNotifyListener()
        {
            var lobbyName = TestContext.CurrentContext.Test.Name;
            var lobbyId = -1;
            var joinerUsername = string.Empty;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("2v2v4", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobbyId = lobby.Id;

                        var listener = new Mock<ILobbyListener>();
                        listener.Setup(lobbyListener => lobbyListener.OnMemberJoined(It.IsAny<LobbyMemberData>()))
                            .Callback((LobbyMemberData memberData) =>
                            {
                                memberData.Username.ShouldBe(joinerUsername);
                                done.Set();
                            });

                        lobby.SetListener(listener.Object);

                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created

            var lobbyJoiner = new SpeedDateClient();
            lobbyJoiner.Started += () =>
            {
                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    joinerUsername = info.Username;
                    lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobbyId, joinedLobby =>
                    {
                        //Listener will signal done
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobby-listener
        }

        [Test]
        public void LeaveLobby_ShouldNotifyListener()
        {
            var lobbyId = -1;
            var joinerUsername = string.Empty;
            var lobbyName = TestContext.CurrentContext.Test.Name;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("2v2v4", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobbyId = lobby.Id;

                        var listener = new Mock<ILobbyListener>();
                        listener.Setup(lobbyListener => lobbyListener.OnMemberLeft(It.IsAny<LobbyMemberData>()))
                            .Callback((LobbyMemberData memberData) =>
                            {
                                memberData.Username.ShouldBe(joinerUsername);
                                done.Set();
                            });

                        lobby.SetListener(listener.Object);

                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created

            var lobbyJoiner = new SpeedDateClient();
            lobbyJoiner.Started += () =>
            {
                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    joinerUsername = info.Username;
                    lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobbyId, joinedLobby =>
                    {
                        lobbyJoiner.GetPlugin<LobbyPlugin>().LeaveLobby(lobbyId, () =>
                        {
                            //Listener will signal done
                        }, error => { throw new Exception(error); });
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobby-listener
        }


        [Test]
        public void FindGames_ShouldContainLobby()
        {
            var lobbyName = TestContext.CurrentContext.Test.Name;
            var lobbyId = -1;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("2v2v4", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobbyId = lobby.Id;
                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created

            var lobbyJoiner = new SpeedDateClient();
            lobbyJoiner.Started += () =>
            {
                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyJoiner.GetPlugin<MatchmakerPlugin>().FindGames(new Dictionary<string, string>(), games =>
                    {
                        games.ShouldContain(packet => packet.Id.Equals(lobbyId));

                        var lobby = games.First(packet => packet.Id.Equals(lobbyId));
                        lobby.Type.ShouldBe(GameInfoType.Lobby);
                        lobby.Name.ShouldBe(lobbyName);
                        lobby.OnlinePlayers.ShouldBe(1);

                        done.Set();

                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined
        }

        [Test]
        public void AutoLobbyFindGames_ShouldContainLobby()
        {
            var lobbyId = -1;
            var lobbyName = TestContext.CurrentContext.Test.Name;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("3v3auto", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobbyId = lobby.Id;
                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created

            var lobbyJoiner = new SpeedDateClient();
            lobbyJoiner.Started += () =>
            {
                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyJoiner.GetPlugin<MatchmakerPlugin>().FindGames(new Dictionary<string, string>(), games =>
                    {
                        games.ShouldContain(packet => packet.Id.Equals(lobbyId));

                        var gameInfo = games.First(packet => packet.Id.Equals(lobbyId));
                        gameInfo.Type.ShouldBe(GameInfoType.Lobby);
                        gameInfo.Name.ShouldBe(lobbyName);
                        gameInfo.OnlinePlayers.ShouldBe(1);
                        done.Set();

                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined
        }

        [Test]
        public void JoinTeam_CreatorShouldBeInSameTeam()
        {
            var lobbyId = -1;
            var lobbyName = TestContext.CurrentContext.Test.Name;

            var joinerTeam = string.Empty;
            var creatorTeam = string.Empty;
            var joinerUsername = string.Empty;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("3v3auto", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        creatorTeam = lobby.Members[info.Username].Team;
                        creatorTeam.ShouldNotBeNullOrEmpty();

                        lobbyId = lobby.Id;

                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created

            var lobbyJoiner = new SpeedDateClient();
            lobbyJoiner.Started += () =>
            {
                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobbyId, joinedLobby =>
                    {
                        lobbyJoiner.GetPlugin<LobbyPlugin>().LastJoinedLobby.ShouldBe(joinedLobby);

                        joinerTeam = joinedLobby.Members[info.Username].Team;
                        joinerTeam.ShouldNotBeNullOrEmpty();
                        joinerUsername = info.Username;

                        var joinedLobbyListenerMock = new Mock<ILobbyListener>();
                        joinedLobbyListenerMock.Setup(listener =>
                                listener.OnMemberTeamChanged(It.IsAny<LobbyMemberData>(), It.IsAny<LobbyTeamData>()))
                            .Callback((LobbyMemberData member, LobbyTeamData team) =>
                            {
                                team.Name.ShouldBe(creatorTeam);
                                member.Username.ShouldBe(info.Username);
                                done.Set();
                            });
                        joinedLobby.SetListener(joinedLobbyListenerMock.Object);
                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined

            //At this point, the joiner and creator should be in different teams
            //Team A:
            //  Creator
            //Team B:
            //  Joiner
            creatorTeam.ShouldNotBe(joinerTeam);
            lobbyJoiner.GetPlugin<LobbyPlugin>().LastJoinedLobby.Members[joinerUsername].Team.ShouldBe(joinerTeam);

            //Let joiner switch to the creator's team
            lobbyJoiner.GetPlugin<LobbyPlugin>().JoinTeam(lobbyId, creatorTeam, () => { },
                error => { throw new Exception(error); });

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for OnMemberTeamChanged

            //At this point, the joiner has joined the creator's lobby and switched to his team
            //Team A:
            //  Creator
            //  Joiner
            //Team B:
            //  <empty>            
            lobbyJoiner.GetPlugin<LobbyPlugin>().LastJoinedLobby.Members[joinerUsername].Team.ShouldBe(creatorTeam);
        }

        [Test]
        public void RejoinLobby_CreatorShouldBeInDifferentTeam()
        {
            var lobbyId = -1;
            var lobbyName = TestContext.CurrentContext.Test.Name;

            var joinerTeam = string.Empty;
            var creatorTeam = string.Empty;
            var joinerUsername = string.Empty;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("3v3auto", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobby.Members.ShouldContainKey(info.Username);

                        lobbyId = lobby.Id;
                        creatorTeam = lobby.Members[info.Username].Team;
                        creatorTeam.ShouldNotBeNullOrEmpty();

                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created

            var lobbyJoiner = new SpeedDateClient();
            lobbyJoiner.Started += () =>
            {
                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    joinerUsername = info.Username;
                    lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobbyId, joinedLobby =>
                    {
                        joinedLobby.Members.ShouldContainKey(info.Username);

                        joinerTeam = joinedLobby.Members[info.Username].Team;
                        joinerTeam.ShouldNotBeNullOrEmpty();

                        var joinedLobbyListenerMock = new Mock<ILobbyListener>();
                        joinedLobbyListenerMock.Setup(listener =>
                                listener.OnMemberTeamChanged(It.IsAny<LobbyMemberData>(),
                                    It.IsAny<LobbyTeamData>()))
                            .Callback((LobbyMemberData member, LobbyTeamData team) =>
                            {
                                team.Name.ShouldBe(creatorTeam);
                                member.Username.ShouldBe(info.Username);
                                done.Set();
                            });

                        joinedLobby.SetListener(joinedLobbyListenerMock.Object);
                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined

            lobbyJoiner.GetPlugin<LobbyPlugin>().LeaveLobby(lobbyId, () => done.Set(),
                error => { throw new Exception(error); });

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, LeaveLobby

            //Rejoin lobby
            lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobbyId, joinedLobby =>
            {
                joinedLobby.Members.ShouldContainKey(joinerUsername);

                joinerTeam = joinedLobby.Members[joinerUsername].Team;
                joinerTeam.ShouldNotBeNullOrEmpty();

                done.Set();
            }, error => { throw new Exception(error); });

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, JoinLobby

            joinerTeam.ShouldNotBe(creatorTeam);
        }

        [Test]
        public void JoinTeamThenRejoinLobby_CreatorShouldBeInDifferentTeam()
        {
            var lobbyId = -1;
            var lobbyName = TestContext.CurrentContext.Test.Name;

            var joinerTeam = string.Empty;
            var creatorTeam = string.Empty;
            var joinerUsername = string.Empty;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("3v3auto", new Dictionary<string, string>
                    {
                        {
                            OptionKeys.LobbyName, lobbyName
                        }
                    }, lobby =>
                    {
                        lobby.Members.ShouldContainKey(info.Username);

                        creatorTeam = lobby.Members[info.Username].Team;
                        creatorTeam.ShouldNotBeNullOrEmpty();
                        lobbyId = lobby.Id;

                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyCreator.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created

            var lobbyJoiner = new SpeedDateClient();
            lobbyJoiner.Started += () =>
            {
                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    joinerUsername = info.Username;
                    lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobbyId, joinedLobby =>
                    {
                        joinedLobby.Members.ShouldContainKey(info.Username);

                        joinerTeam = joinedLobby.Members[info.Username].Team;
                        joinerTeam.ShouldNotBeNullOrEmpty();

                        var joinedLobbyListenerMock = new Mock<ILobbyListener>();
                        joinedLobbyListenerMock.Setup(listener =>
                                listener.OnMemberTeamChanged(It.IsAny<LobbyMemberData>(),
                                    It.IsAny<LobbyTeamData>()))
                            .Callback((LobbyMemberData member, LobbyTeamData team) =>
                            {
                                team.Name.ShouldBe(creatorTeam);
                                member.Username.ShouldBe(info.Username);
                                done.Set();
                            });

                        joinedLobby.SetListener(joinedLobbyListenerMock.Object);
                        done.Set();
                    }, error => { throw new Exception(error); });
                }, error => { throw new Exception(error); });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined

            //Let joiner switch to the creator's team
            lobbyJoiner.GetPlugin<LobbyPlugin>().JoinTeam(lobbyId, creatorTeam, () => { }, error => { throw new Exception(error); });

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for OnMemberTeamChanged

            lobbyJoiner.GetPlugin<LobbyPlugin>().LeaveLobby(lobbyId, () => done.Set(), error => { throw new Exception(error); });

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, LeaveLobby

            //Rejoin lobby
            lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobbyId, joinedLobby =>
            {
                joinedLobby.Members.ShouldContainKey(joinerUsername);

                joinerTeam = joinedLobby.Members[joinerUsername].Team;
                joinerTeam.ShouldNotBeNullOrEmpty();

                done.Set();
            }, error => { throw new Exception(error); });

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, JoinLobby

            joinerTeam.ShouldNotBe(creatorTeam);
        }
    }
}
