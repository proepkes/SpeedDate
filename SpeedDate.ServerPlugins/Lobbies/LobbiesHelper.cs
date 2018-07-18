using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SpeedDate.Configuration;
using SpeedDate.Packets.Lobbies;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public static class LobbiesHelper
    {
        public static string ExtractLobbyName(this Dictionary<string, string> properties)
        {
            return properties.ContainsKey(OptionKeys.LobbyName) ? properties[OptionKeys.LobbyName] : Lobby.DefaultName;
        }

        public static LobbyBuilder CreateLobbyBuilder(StringReader reader)
        {
            var config = reader.ReadToEnd();

            return (plugin, properties, creator) =>
            {
                Lobby result = null;

                var xmlReader = new XmlParser(config);

                var teams = new List<LobbyTeam>();
                xmlReader.SearchEach("Team", () =>
                {
                    teams.Add(new LobbyTeam(xmlReader["Name"])
                    {
                        MinPlayers = Convert.ToInt32(xmlReader["MinPlayers"]),
                        MaxPlayers = Convert.ToInt32(xmlReader["MaxPlayers"])
                    });
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
                        StartGameWhenAllReady = Convert.ToBoolean(xmlReader["StartGameWhenAllReady"]),
                        Name = properties.ExtractLobbyName()
                    };
                });

                xmlReader = new XmlParser(config);
                xmlReader.SearchEach("Control", () =>
                {
                    var defaultValue = "";
                    var control = new LobbyPropertyData
                    {
                        PropertyKey = xmlReader["Key"],
                        Label = xmlReader["Label"],
                        Options = new List<string>()
                    };
                    xmlReader.SearchEach("Controloption", () =>
                    {
                        var value = xmlReader["Value"];
                        if (xmlReader["IsDefault"] != null)
                        {
                            defaultValue = value;
                        }
                        control.Options.Add(value);
                    });
                    result.AddControl(control, defaultValue);
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
