using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SpeedDate.Configuration;

namespace SpeedDate.Server.Console
{
    class Program
    {/// <summary>
     ///     The server instance.
     /// </summary>

        /// <summary>
        ///     Main entry point of the server which starts a single server.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var speedDate = new SpeedDater();
            speedDate.Start(new FileConfigProvider("ServerConfig.xml"));

            System.Console.ReadLine();

            ////new Thread(new ThreadStart(ConsoleLoop)).Start();


            //var input = "";
            //while (input != "x")
            //{
            //input = System.Console.ReadLine()?.ToLower();
            //    _masterServer.HandleCommand(input);   
            //}
            //while (true)
            //{
            //    server.DispatcherWaitHandle.WaitOne();
            //    server.ExecuteDispatcherTasks();
            //}
        }

        /// <summary>
        ///     Invoked from another thread to repeatedly execute commands from the console.
        /// </summary>
        static void ConsoleLoop()
        {
            //while (true)
            //{
            //    string input = System.Console.ReadLine();

            //    server.ExecuteCommand(input);
            //}
        }
    }
}
