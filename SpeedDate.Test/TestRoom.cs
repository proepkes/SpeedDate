using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.GameServer;
using SpeedDate.Configuration;
using SpeedDate.Packets.Rooms;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestRoom
    {
        [Test]
        public void RegisterRoom_ShouldApplyOptions()
        {
            var done = new AutoResetEvent(false);

            var roomOptions = new RoomOptions
            {
                IsPublic = true,
                MaxPlayers = 8,
                Name = "Testroom",
                Password = "TestPassword",
                RoomIp = "127.42.42.42",
                RoomPort = 424242,
                AllowUsersRequestAccess = true,
                AccessTimeoutPeriod = 10f,
                Properties = new Dictionary<string, string> { { "MyRoomOption", "123"} }
            };

            //GameServer's client to Masterserver
            var gameServer = new SpeedDateClient();
            gameServer.Started += () =>
            {
                gameServer.IsConnected.ShouldBeTrue();
                gameServer.GetPlugin<RoomsPlugin>().RegisterRoom(
                    roomOptions,
                    info =>
                    {
                        info.Options.IsPublic.ShouldBe(roomOptions.IsPublic);
                        info.Options.MaxPlayers.ShouldBe(roomOptions.MaxPlayers);
                        info.Options.Name.ShouldBe(roomOptions.Name);
                        info.Options.Password.ShouldBe(roomOptions.Password);
                        info.Options.RoomIp.ShouldBe(roomOptions.RoomIp);
                        info.Options.RoomPort.ShouldBe(roomOptions.RoomPort);
                        info.Options.AllowUsersRequestAccess.ShouldBe(roomOptions.AllowUsersRequestAccess);
                        info.Options.Properties.Count.ShouldBe(1);
                        info.Options.Properties.ShouldContainKeyAndValue("MyRoomOption", "123");

                        gameServer.GetPlugin<RoomsPlugin>().GetLocallyCreatedRooms().ShouldContain(info);
                        gameServer.GetPlugin<RoomsPlugin>().GetRoomController(info.RoomId).ShouldBe(info);

                        done.Set();
                    },
                    error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
            };

            gameServer.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultGameServerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside GetJoinedChannels
        }

        [Test]
        public void MakeRoomPublic_ShouldMakePublic()
        {
            var done = new AutoResetEvent(false);

            var roomOptions = new RoomOptions
            {
                IsPublic = false,
            };

            //GameServer's client to Masterserver
            var gameServer = new SpeedDateClient();
            gameServer.Started += () =>
            {
                gameServer.IsConnected.ShouldBeTrue();
                gameServer.GetPlugin<RoomsPlugin>().RegisterRoom(
                    roomOptions,
                    info =>
                    {
                        info.Options.IsPublic.ShouldBe(false);

                        info.MakePublic(() =>
                        {
                            info.Options.IsPublic.ShouldBe(true);
                            done.Set();
                        });
                    },
                    error =>
                    {
                        Should.NotThrow(() => throw new Exception(error));
                    });
            };

            gameServer.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultGameServerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled inside GetJoinedChannels
        }
    }
}
