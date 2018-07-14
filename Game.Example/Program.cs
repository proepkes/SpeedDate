using System;
using ConsoleGame;
using SpeedDate;

namespace Game.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine($"Starting game with arguments: {string.Join(", ", args)}...");

            var server = new GameServer();
            server.ConnectedToMaster += () =>
            {
                Console.WriteLine("Connected to Master");
                server.Rooms.RegisterSpawnedProcess(
                    CommandLineArgs.SpawnId,
                    CommandLineArgs.SpawnCode,
                    (controller) =>
                    {
                        Console.WriteLine("Registered to Master");
                    }, Console.WriteLine);
            };

            server.Start("GameServerConfig.xml");

            Console.ReadLine();
        }
    }
}
