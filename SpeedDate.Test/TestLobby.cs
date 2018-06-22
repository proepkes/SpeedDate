using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Auth;
using SpeedDate.ClientPlugins.Peer.Lobby;
using SpeedDate.ClientPlugins.Peer.MatchMaker;
using SpeedDate.Configuration;
using SpeedDate.Packets.Matchmaking;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Lobbies;
using Xunit;
using Xunit.Abstractions;

namespace SpeedDate.Test
{
    public class TestLobby
    {
        private readonly ITestOutputHelper _output;

        public TestLobby(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(13000)]
        public void TestCreateAndJoinLobby(int port)
        {
            const string LOBBY_NAME = "MyTestLobby";
            const int EXPECTED_LOBBY_ID = 0;

            var are = new AutoResetEvent(false);

            var server = new SpeedDateServer();
            var lobbyCreator = new SpeedDateClient();
            lobbyCreator.Started += () =>
            {
//                lobbyCreator.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
//                {
//                    _output.WriteLine($"Logged in as {info.Username}");
//                    lobbyCreator.GetPlugin<LobbyPlugin>().CreateAndJoin("3 vs 3", new Dictionary<string, string> {
//                    {
//                        OptionKeys.LobbyName, LOBBY_NAME
//                    }}, lobby =>
//                    {
//                        lobby.LobbyName.ShouldBe(LOBBY_NAME);
//                        lobby.State.ShouldBe(LobbyState.Preparations);
//                        lobby.Id.ShouldBe(EXPECTED_LOBBY_ID);
//
//                        _output.WriteLine($"Lobby successfully created");
                        are.Set();
//                    }, error =>
//                    {
//                        Should.NotThrow(() => throw new Exception(error));
//                    });
//                },
//                error =>
//                {
//                    Should.NotThrow(() => throw new Exception(error));
//                });
            };

            server.Started += () =>
            {
                _output.WriteLine($"Server started");
                lobbyCreator.Start(new DefaultConfigProvider(
                    new NetworkConfig(IPAddress.Loopback, port), //Connect to port
                    new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only
            };

            server.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Any, port), //Listen on port
                new PluginsConfig("SpeedDate.ServerPlugins.*"), //Load server-plugins only
                new IConfig[] {
                    new AuthConfig
                    {
                        EnableGuestLogin = true
                    }
                })
            );
//
//            server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("3 vs 3", server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.TwoVsTwoVsFour));
//
//            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue(); //Should be signaled, wait for lobbby-created
//
//            _output.WriteLine($"Starting LobbyJoiner...");
//
//            var lobbyJoiner = new SpeedDateClient();
//            lobbyJoiner.Started += () =>
//            {
//                lobbyJoiner.GetPlugin<AuthPlugin>().LogInAsGuest(info =>
//                    {
//                        _output.WriteLine($"Logged in as {info.Username}");
//                        lobbyJoiner.GetPlugin<MatchmakerPlugin>().FindGames(new Dictionary<string, string>(), games =>
//                        {
//                            games.Count.ShouldBe(1);
//
//                            var lobby = games.First();
//                            lobby.Type.ShouldBe(GameInfoType.Lobby);
//                            lobby.Id.ShouldBe(EXPECTED_LOBBY_ID);
//                            lobby.Name.ShouldBe(LOBBY_NAME);
//                            lobby.OnlinePlayers.ShouldBe(1);
//
//                            lobbyJoiner.GetPlugin<LobbyPlugin>().JoinLobby(lobby.Id, joinedLobby =>
//                            {
//                                joinedLobby.Id.ShouldBe(EXPECTED_LOBBY_ID);
//                                joinedLobby.Members.Count.ShouldBe(2);
//
//                                _output.WriteLine($"Lobby successfully joined");
//                                are.Set();
//                            }, error =>
//                            {
//                                Should.NotThrow(() => throw new Exception(error));
//                            });
//                        }, error =>
//                        {
//                            Should.NotThrow(() => throw new Exception(error));
//                        });
//                    },
//                    error =>
//                    {
//                        Should.NotThrow(() => throw new Exception(error));
//                    });
//            };
//
//            lobbyJoiner.Start(new DefaultConfigProvider(
//                new NetworkConfig(IPAddress.Loopback, port), //Connect to port
//                new PluginsConfig("SpeedDate.ClientPlugins.Peer*"))); //Load peer-plugins only
//
            are.WaitOne(TimeSpan.FromSeconds(10)).ShouldBeTrue(); //Should be signaled, wait for lobbby-joined

            server.Stop();
            server.Dispose();
        }
    }
}
