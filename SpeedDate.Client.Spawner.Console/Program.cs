using SpeedDate.Packets.Spawner;

namespace SpeedDate.Client.Spawner.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("------STARTING SPAWNER------");
            var spawner = new Spawner("SpawnerConfig.xml");
            spawner.Start();
            spawner.Connected += () => spawner.SpawnApi.RegisterSpawner(new SpawnerOptions { Region = "EU" },
                (controller, error) =>
                {
                    System.Console.WriteLine("Registered spawner");

                });
            System.Console.ReadLine();


        }
    }
}
