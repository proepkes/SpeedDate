using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeedDate;

namespace ConsoleGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Starting game with arguments: {string.Join(", ", args)}...");

            var server = new GameServer("GameServerConfig.xml");
            server.ConnectedToMaster += () =>
            {
                Console.WriteLine("Connected to Master");
                server.Rooms.RegisterSpawnedProcess(
                    CommandLineArgs.SpawnId,
                    CommandLineArgs.SpawnCode,
                    (controller, error) =>
                    {
                        Console.WriteLine(error ?? "Registered to Master");
                    });
            };

            server.Start();

            Console.ReadLine();
        }
    }
}
