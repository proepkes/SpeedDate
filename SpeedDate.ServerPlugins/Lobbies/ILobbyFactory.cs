using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public interface ILobbyFactory
    {
        string Id { get; }

        ILobby CreateLobby(Dictionary<string, string> properties, IPeer creator);
    }
}