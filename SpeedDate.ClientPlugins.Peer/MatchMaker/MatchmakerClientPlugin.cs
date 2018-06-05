using System.Collections.Generic;
using System.Linq;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Networking;
using SpeedDate.Packets.Matchmaking;

namespace SpeedDate.ClientPlugins.Peer.MatchMaker
{
    public delegate void FindGamesCallback(List<GameInfoPacket> games);

    public class MatchmakerClientPlugin : SpeedDateClientPlugin
    {
        public MatchmakerClientPlugin(IClientSocket connection) : base(connection)
        {
        }


        /// <summary>
        ///     Retrieves a list of public games, which pass a provided filter.
        ///     (You can implement your own filtering by extending modules or "classes"
        ///     that implement <see cref="IGamesProvider" />)
        /// </summary>
        public void FindGames(Dictionary<string, string> filter, FindGamesCallback callback)
        {
            if (!Connection.IsConnected)
            {
                Logs.Error("Not connected");
                callback.Invoke(new List<GameInfoPacket>());
                return;
            }

            Connection.SendMessage((short) OpCodes.FindGames, filter.ToBytes(), (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    Logs.Error(response.AsString("Unknown error while requesting a list of games"));
                    callback.Invoke(new List<GameInfoPacket>());
                    return;
                }

                var games = response.DeserializeList(() => new GameInfoPacket()).ToList();

                callback.Invoke(games);
            });
        }
    }
}