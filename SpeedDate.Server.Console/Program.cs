using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SpeedDate.Configuration;

namespace SpeedDate.Server.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new SpeedDateServer();
            server.Start(new FileConfigProvider("ServerConfig.xml"));

            System.Console.ReadLine();
        }
    }
}
