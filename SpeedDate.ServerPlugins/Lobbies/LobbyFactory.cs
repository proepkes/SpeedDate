using System;
using System.Collections.Generic;
using System.IO;
using SpeedDate.Configuration;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Lobbies;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public class LobbyFactory
    {
        public static Func<LobbiesPlugin, Dictionary<string, string>, IPeer, Lobby> FromFile(string file)
        {
            var config = File.ReadAllText(file);

            return (plugin, properties, creator) =>
            {
                Lobby result = null;
                var xmlReader = new XmlParser(config);

                var teams = new List<LobbyTeam>();
                xmlReader.SearchEach("Team", () =>
                {
                    teams.Add(new LobbyTeam(xmlReader["Name"]));
                });

                xmlReader = new XmlParser(config);
                xmlReader.Search("Lobby", () =>
                {
                    result = new Lobby(plugin.GenerateLobbyId(), teams, plugin)
                    {
                        DisplayName = xmlReader["DisplayName"],
                        Autostart = Convert.ToBoolean(xmlReader["Autostart"]),
                        AllowJoiningWhenGameIsLive = Convert.ToBoolean(xmlReader["AllowJoiningWhenGameIsLive"]),
                        AllowPlayersChangeLobbyProperties =
                            Convert.ToBoolean(xmlReader["AllowPlayersChangeLobbyProperties"]),
                        KeepAliveWithZeroPlayers = Convert.ToBoolean(xmlReader["KeepAliveWithZeroPlayers"]),
                        EnableGameMasters = Convert.ToBoolean(xmlReader["EnableGameMasters"]),
                        EnableManualStart = Convert.ToBoolean(xmlReader["EnableManualStart"]),
                        EnableReadySystem = Convert.ToBoolean(xmlReader["EnableReadySystem"]),
                        EnableTeamSwitching = Convert.ToBoolean(xmlReader["EnableTeamSwitching"]),
                        PlayAgainEnabled = Convert.ToBoolean(xmlReader["PlayAgainEnabled"]),
                        StartGameWhenAllReady = Convert.ToBoolean(xmlReader["StartGameWhenAllReady"])
                    };
                });

                xmlReader = new XmlParser(config);
                xmlReader.SearchEach("Control", () =>
                {
                    var control = new LobbyPropertyData
                    {
                        PropertyKey = xmlReader["Key"],
                        Label = xmlReader["Label"],
                        Options = new List<string>()
                    };
                    xmlReader.SearchEach("Controloption", () =>
                    {
                        control.Options.Add(xmlReader["Value"]);
                    });
                    result.AddControl(control);
                });


                if (result.Autostart)
                {
                    result.StartAutomation();
                }

                return result;
            };
        }
    }
}


