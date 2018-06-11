using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Common;
using SpeedDate.Packets.Lobbies;

namespace SpeedDate.ClientPlugins.GameServer
{
    public class LobbiesPlugin : SpeedDateClientPlugin
    {
        public delegate void LobbyMemberDataCallback(LobbyMemberData memberData, string error);

        public delegate void LobbyInfoCallback(LobbyDataPacket info, string error);
        
        /// <summary>
        /// Retrieves lobby member data of user, who has connected to master server with
        /// a specified peerId
        /// </summary>
        public void GetMemberData(int lobbyId, int peerId, LobbyMemberDataCallback callback)
        {
            var packet = new IntPairPacket
            {
                A = lobbyId,
                B = peerId
            };

            Connection.SendMessage((short) OpCodes.GetLobbyMemberData, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown error"));
                    return;
                }

                var memberData = response.Deserialize(new LobbyMemberData());
                callback.Invoke(memberData, null);
            });
        }
        
        /// <summary>
        /// Retrieves information about the lobby
        /// </summary>
        public void GetLobbyInfo(int lobbyId, LobbyInfoCallback callback)
        {
            Connection.SendMessage((short)OpCodes.GetLobbyInfo, lobbyId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown error"));
                    return;
                }

                var memberData = response.Deserialize(new LobbyDataPacket());
                callback.Invoke(memberData, null);
            });
        }
    }
}