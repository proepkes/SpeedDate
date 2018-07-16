using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public interface ILobbyFactory
    {
        string Id { get; }

        Lobby CreateLobby(Dictionary<string, string> properties, IPeer creator);
    }
}