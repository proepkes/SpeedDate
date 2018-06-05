using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Networking;
using SpeedDate.Packets.Matchmaking;

namespace SpeedDate.ServerPlugins.Matchmaker
{
    public interface IGamesProvider
    {
        IEnumerable<GameInfoPacket> GetPublicGames(IPeer peer, Dictionary<string, string> filters);
    }
}