using System.Collections.Generic;
using System.Linq;

using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Matchmaking;

namespace SpeedDate.ClientPlugins.Peer.MatchMaker
{
    public delegate void FindGamesCallback(List<GameInfoPacket> games);

    public class MatchmakerPlugin : SpeedDateClientPlugin
    {
        /// <summary>
        ///     Retrieves a list of public games, which pass a provided filter.
        ///     (You can implement your own filtering by extending modules or "classes"
        ///     that implement <see cref="IGamesProvider" />)
        /// </summary>
        public void FindGames(Dictionary<string, string> filter, FindGamesCallback callback, ErrorCallback errorCallback)
        {
            if (!Connection.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            Connection.SendMessage((ushort) OpCodes.FindGames, filter.ToBytes(), (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error while requesting a list of games"));
                    return;
                }

                var games = response.DeserializeList(() => new GameInfoPacket()).ToList();

                callback.Invoke(games);
            });
        }
    }
}
