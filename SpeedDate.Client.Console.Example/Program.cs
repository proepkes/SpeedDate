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
            System.Console.WriteLine("------STARTING GAMECLIENT------");
            var gameClient = new GameClient("GameClientConfig.xml");
            gameClient.Start();
            gameClient.Connected += () =>
            {
                gameClient.Auth.LogInAsGuest((info, e) =>
                {
                    System.Console.WriteLine($"Player logged in as : {info.Username}");

                    gameClient.Spawn.RequestSpawn(new Dictionary<string, string>(), "EU", (controller, error) =>
                    {
                        System.Console.WriteLine(error ?? "Spawned");
                    });
                });
            };
            System.Console.ReadLine();


        }
    }
}
