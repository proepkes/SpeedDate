using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SpeedDate.Configuration;

namespace SpeedDate.ServerPlugins.Lobbies
{
    class LobbiesConfig : IConfig
    {
        public string LobbyFiles { get; set; } = string.Empty;

        public IEnumerable<(string filename, string content)> ReadAllFiles()
        {
            var result = new List<(string, string)>();
            foreach (var file in LobbyFiles.Split(';'))
            {
                var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var fileName = $"{filePath}\\{file}";
                
                if (!File.Exists(fileName))
                {
                    fileName = $"{filePath}\\{file}.lobby";
                    if (!File.Exists(fileName)) //Search for file.lobby
                    {
                        fileName = $"{filePath}\\Lobbies\\{file}";
                        if (!File.Exists(fileName)) //Search for Lobbies\file
                        {
                            fileName = $"{filePath}\\Lobbies\\{file}.lobby";
                            if (!File.Exists(fileName)) //Search for Lobbies\file.lobby
                            {
                                continue;
                            }
                        }
                    }
                }

                result.Add((file, File.ReadAllText(fileName)));
            }

            return result;
        }
    }
}
