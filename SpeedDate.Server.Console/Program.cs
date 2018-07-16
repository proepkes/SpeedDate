using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SpeedDate.Configuration;
using SpeedDate.ServerPlugins.Lobbies;

namespace SpeedDate.Server.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new SpeedDateServer();
            server.Start(new FileConfigProvider("ServerConfig.xml"));

            server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("Deathmatch", server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.Deathmatch));
            server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("2v2v4", server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.TwoVsTwoVsFour));
            server.GetPlugin<LobbiesPlugin>().AddFactory(new LobbyFactoryAnonymous("3v3auto", server.GetPlugin<LobbiesPlugin>(), DemoLobbyFactories.ThreeVsThreeQueue));
            System.Console.ReadLine();
        }
    }
}
