using System.Collections.Generic;
using SpeedDate.Configuration;

namespace SpeedDate.Client.Console.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            System.Console.WriteLine("------STARTING GAMECLIENT------");
            var gameClient = new GameClient();
            gameClient.Connected += () =>
            {
                gameClient.Auth.LogInAsGuest(info =>
                    {
                        System.Console.WriteLine($"Player logged in as : {info.Username}");

                        gameClient.Spawn.RequestSpawn(new Dictionary<string, string>(), "EU",
                            controller => { System.Console.WriteLine("Spawned"); }, System.Console.WriteLine);
                    },
                    error => { });
            };

            gameClient.Start(new DefaultConfigProvider(new NetworkConfig("localhost", 60125), PluginsConfig.LoadAllPlugins));
            System.Console.ReadLine();
        }
    }
}
