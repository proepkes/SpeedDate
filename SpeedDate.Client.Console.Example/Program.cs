using System;
using System.Collections.Generic;
using System.Threading;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.Client.Console.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("------STARTING SPAWNER------");
            var spawner = new Spawner("SpawnerConfig.xml");
            spawner.Start();
            spawner.Connected += () => spawner.Spawn.RegisterSpawner(new SpawnerOptions { Region = "EU" },
                (controller, error) =>
                {
                    System.Console.WriteLine("Registered spawner");

                });

            Thread.Sleep(TimeSpan.FromSeconds(1));
            System.Console.WriteLine("------STARTING GAMECLIENT------");
            var gameClient = new GameClient("GameClientConfig.xml");
            gameClient.Start();
            gameClient.Connected += () =>
            {
                gameClient.Auth.LogInAsGuest((info, error) =>
                {
                    System.Console.WriteLine($"Player logged in as : {info.Username}");

                    gameClient.Spawn.RequestSpawn(new Dictionary<string, string>(), "EU", (controller, s) =>
                    {
                        System.Console.WriteLine("Spawned");
                    });
                });
            };
            System.Console.ReadLine();


        }
    }
}
