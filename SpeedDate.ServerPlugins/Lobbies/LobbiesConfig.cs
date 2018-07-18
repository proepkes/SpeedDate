using System;
using System.Collections.Generic;
using System.IO;
using SpeedDate.Configuration;

namespace SpeedDate.ServerPlugins.Lobbies
{
    class LobbiesConfig : IConfig
    {
        public string LobbyFiles { get; set; } = string.Empty;

        public IEnumerable<string> ReadAllFiles()
        {
            var result = new List<string>();
            foreach (var file in LobbyFiles.Split(';'))
            {
                var fileName = file;
                if (!File.Exists(fileName))
                {
                    fileName = $"{file}.lobby";
                    if (!File.Exists(fileName)) //Search for file.lobby
                    {
                        fileName = $"Lobbies\\{file}";
                        if (!File.Exists(fileName)) //Search for Lobbies\file
                        {
                            fileName = $"Lobbies\\{file}.lobby";
                            if (!File.Exists(fileName)) //Search for Lobbies\file.lobby
                            {
                                continue;
                            }
                        }
                    }
                }

                result.Add(File.ReadAllText(fileName));
            }

            return result;
        }
    }
}
