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
            const int EXPECTED_LOBBY_ID = 0;

            var are = new AutoResetEvent(false);

            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                {
                    info.Username.ShouldNotBeNullOrEmpty();
                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("3 vs 3", new Dictionary<string, string> {
                    {
                        OptionKeys.LobbyName, LOBBY_NAME
                    }}, lobby =>
                    {
                        lobby.LobbyName.ShouldBe(LOBBY_NAME);
                        lobby.State.ShouldBe(LobbyState.Preparations);
                        lobby.Id.ShouldBe(EXPECTED_LOBBY_ID);
                        
                        are.Set();
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
                new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only

            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created


            var lobbyJoiner = new SpeedDateClient();
            lobbyJoiner.Started += () =>
            {
                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
                    {
                        info.Username.ShouldNotBeNullOrEmpty();
                        lobbyJoiner.GetPlugin<MatchmakerPlugin>().FindGames(new Dictionary<string, string>(), games =>
                        {
                            games.Count.ShouldBe(1);

                            var lobby = games.First();
                            lobby.Type.ShouldBe(GameInfoType.Lobby);
                            lobby.Id.ShouldBe(EXPECTED_LOBBY_ID);
                            lobby.Name.ShouldBe(LOBBY_NAME);
                            lobby.OnlinePlayers.ShouldBe(1);

                            lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobby.Id, joinedLobby =>
                            {
                                joinedLobby.Id.ShouldBe(EXPECTED_LOBBY_ID);
                                joinedLobby.Members.Count.ShouldBe(2);

                                are.Set();
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
                new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only

            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined
        }
    }
}
