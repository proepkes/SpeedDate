using System.Collections.Generic;
using SpeedDate.Network.Interfaces;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public delegate Lobby LobbyBuilder(LobbiesPlugin plugin, Dictionary<string, string> properties, IPeer creator);

    
}