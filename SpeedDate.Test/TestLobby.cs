using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Lobby;
using SpeedDate.ClientPlugins.Peer.MatchMaker;
using SpeedDate.Configuration;
using SpeedDate.Packets.Matchmaking;
using LobbyState = SpeedDate.Packets.Lobbies.LobbyState;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestLobby
    {
        [Test]
        public void TestCreateAndJoinLobby()
        {
            const string LOBBY_NAME = "MyTestLobby";
            var lobbyId = -1;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    info.Username.ShouldNotBeNullOrEmpty();
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("2v2v4", new Dictionary<string, string> {
                    {
                        OptionKeys.LobbyName, LOBBY_NAME
                    }}, lobby =>
                    {
                        lobby.Data.GameMaster.ShouldBe(info.Username);
                        lobby.LobbyName.ShouldBe(LOBBY_NAME);
                        lobby.State.ShouldBe(LobbyState.Preparations);
                        lobby.Id.ShouldBeGreaterThanOrEqualTo(0);

                        lobbyId = lobby.Id;

                        done.Set();
                    }, error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
                },
                error =>
                {
                    Should.NotThrow(() => throw new Exception(error));
                });
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
                    info.Username.ShouldNotBeNullOrEmpty();
                    lobbyJoiner.GetPlugin<MatchmakerPlugin>().FindGames(new Dictionary<string, string>(), games =>
                    {
                        games.ShouldContain(packet => packet.Id.Equals(lobbyId));

                        var lobby = games.First(packet => packet.Id.Equals(lobbyId));
                        lobby.Type.ShouldBe(GameInfoType.Lobby);
                        lobby.Name.ShouldBe(LOBBY_NAME);
                        lobby.OnlinePlayers.ShouldBe(1);

                        lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobby.Id, joinedLobby =>
                        {
                            joinedLobby.Id.ShouldBe(lobbyId);
                            joinedLobby.Members.Count.ShouldBe(2);

                            done.Set();
                        }, error =>
                        {
                            Should.NotThrow(() => throw new Exception(error));
                        });
                    }, error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
                },
                    error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined
        }

        [Test]
        public void TestCreateAndJoinAutoLobby()
        {
            const string LOBBY_NAME = "MyTestAutoLobby";
            var lobbyId = -1;

            var done = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    info.Username.ShouldNotBeNullOrEmpty();
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("3v3auto", new Dictionary<string, string> {
                    {
                        OptionKeys.LobbyName, LOBBY_NAME
                    }}, lobby =>
                    {
                        //No GameMaster in auto-mode
                        lobby.Data.GameMaster.ShouldBeEmpty();

                        lobby.LobbyName.ShouldBe(LOBBY_NAME);
                        lobby.State.ShouldBe(LobbyState.Preparations);
                        lobby.Id.ShouldBeGreaterThanOrEqualTo(0);

                        lobbyId = lobby.Id;

                        done.Set();
                    }, error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
                },
                error =>
                {
                    Should.NotThrow(() => throw new Exception(error));
                });
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
                        info.Username.ShouldNotBeNullOrEmpty();
                        lobbyJoiner.GetPlugin<MatchmakerPlugin>().FindGames(new Dictionary<string, string>(), games =>
                        {
                            games.ShouldContain(packet => packet.Id.Equals(lobbyId));

                            var lobby = games.First(packet => packet.Id.Equals(lobbyId));
                            lobby.Type.ShouldBe(GameInfoType.Lobby);
                            lobby.Name.ShouldBe(LOBBY_NAME);
                            lobby.OnlinePlayers.ShouldBe(1);

                            lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobby.Id, joinedLobby =>
                            {
                                joinedLobby.Id.ShouldBe(lobbyId);
                                joinedLobby.Members.Count.ShouldBe(2);

                                done.Set();
                            }, error =>
                            {
                                Should.NotThrow(() => throw new Exception(error));
                            });
                        }, error =>
                        {
                            Should.NotThrow(() => throw new Exception(error));
                        });
                    },
                    error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
            };

            lobbyJoiner.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined
        }
    }
}
