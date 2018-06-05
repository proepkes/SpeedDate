using System.Collections.Generic;
using System.Threading;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.Client.Console.Example
{
    class Program
    {
        static void Main(string[] args)
        {

            var spawner = new Spawner("spawner.json");
            spawner.Start();


            //var gameServer = new GameServer("gameserver.json");
            //gameServer.Start();
            
            System.Console.ReadLine();
            //using (var client = new MsfClient(SpeedDateConnection.Socket))
            //{
            //    client.ConnectAsync("127.0.0.1", 60125);

            //    while (!client.ClientSocket.IsConnected)
            //    {
            //        System.Console.WriteLine("Connecting...");
            //        Thread.Sleep(1000);
            //    }


            //    var input = "";
            //    while (input != "x" && client.ClientSocket.IsConnected)
            //    {
            //        input = System.Console.ReadLine()?.ToLower();

            //            switch (input)
            //            {
            //                case "register":
            //                    client.Auth.Register(new Dictionary<string, string>
            //                    {
            //                        { "username", "proepkes" },
            //                        {"password", "pass" },
            //                        {"email", "myMail@mail.com" }
            //                    }, (successful, error) =>
            //                    {
            //                        System.Console.WriteLine(successful);
            //                        System.Console.WriteLine(error);
            //                    });
            //                    break;
            //            }

            //    }
            //}


        }
    }
}
