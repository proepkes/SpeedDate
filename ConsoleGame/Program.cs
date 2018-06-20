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
