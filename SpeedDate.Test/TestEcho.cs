using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using SpeedDate.Client;
using SpeedDate.ClientPlugins.Peer.Echo;
using SpeedDate.Configuration;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestEcho
    {
        [Test]
        public void Echo_ShouldBeEchoed()
        {
            const string message = "MyTestMessage12345";

            var done = new AutoResetEvent(false);

            var client = new SpeedDateClient();
            client.Started += () =>
            {
                client.GetPlugin<EchoPlugin>().Send(message,
                    echo =>
                    {
                        echo.ShouldBe(message);
                        done.Set();
                    },
                    error =>
                    {
                        throw new Exception(error);
                    });
            };

            client.Start(new DefaultConfigProvider(
                new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
        }

        [Test]
        public void EchoFromMultipleClients_ShouldBeEchoed()
        {
            var numberOfClients = 100;

            var done = new AutoResetEvent(false);

            for (var clientNumber = 0; clientNumber < numberOfClients; clientNumber++)
            {
                ThreadPool.QueueUserWorkItem(index =>
                {
                    var client = new SpeedDateClient();
                    client.Started += () =>
                    {
                        client.GetPlugin<EchoPlugin>().Send("Hello from " + index,
                            echo =>
                            {
                                echo.ShouldBe("Hello from " + index);

                                if (Interlocked.Decrement(ref numberOfClients) == 0)
                                    done.Set();
                            },
                            error =>
                            {
                                throw new Exception(error);
                            });
                    };

                    client.Start(new DefaultConfigProvider(
                        new NetworkConfig(IPAddress.Loopback, SetUp.Port), //Connect to port
                        PluginsConfig.DefaultPeerPlugins)); //Load peer-plugins only

                }, clientNumber);
            }

            done.WaitOne(TimeSpan.FromSeconds(30)).ShouldBeTrue(); //Should be signaled
        }
    }
}
