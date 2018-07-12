using SpeedDate.ClientPlugins.Spawner;
using SpeedDate.Configuration;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.Client.Spawner.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("------STARTING SPAWNER------");
            var spawner = new Spawner();
            
            spawner.Connected += () => spawner.SpawnApi.Register(new SpawnerOptions { Region = "EU" },
                controller =>
                {
                    System.Console.WriteLine("Registered spawner");

                }, reason =>
                {
                    System.Console.WriteLine($"Registered failed: {reason}");
                });
            
            //Example configuring in code
//            spawner.Start(
//                new DefaultConfigProvider(
//                    new NetworkConfig("127.0.0.1", 60125), 
//                    new PluginsConfig(false, "SpeedDate.ClientPlugins;SpeedDate.ClientPlugins.Spawner*"), 
//                    new IConfig[] { 
//                        new SpawnerConfig
//                        {
//                            AddWebGlFlag = false,
//                            ExecutablePath = "E:\\Repositories\\SpeedDate\\ConsoleGame\\bin\\Debug\\ConsoleGame.exe",
//                            MachineIp = "127.0.0.1",
//                            SpawnInBatchmode = true
//                        }, })
//            );
            
            
            spawner.Start(new FileConfigProvider("SpawnerConfig.xml"));
            System.Console.ReadLine();


        }
    }
}
