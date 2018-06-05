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
            var spawner = new Spawner("spawner.json");
            spawner.Start();
            spawner.Connected += () => spawner.Spawn.RegisterSpawner(new SpawnerOptions(),
                (controller, error) =>
                {
                    System.Console.WriteLine("Registered spawner");

                });


            System.Console.WriteLine("------STARTING GAMECLIENT------");
            var gameClient = new GameClient("gameclient.json");
            gameClient.Start();
            gameClient.Connected += () =>
            {
                gameClient.Auth.LogInAsGuest((info, error) =>
                {
                    System.Console.WriteLine($"Player logged in as : {info.Username}");
                });
            };
            System.Console.ReadLine();
        }
    }
}
