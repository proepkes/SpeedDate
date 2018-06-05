using SpeedDate.Interfaces;
using SpeedDate.Networking;
using SpeedDate.Packets.Authentication;

namespace SpeedDate.ClientPlugins.GameServer
{
    public class PeerInfoGameServerPlugin : SpeedDateClientPlugin
    {
        public delegate void PeerAccountInfoCallback(PeerAccountInfoPacket info, string error);

        public PeerInfoGameServerPlugin(IClientSocket connection) : base(connection)
        {
        }

        /// <summary>
        /// Gets account information of a client, who is connected to master server, 
        /// and who's peer id matches the one provided
        /// </summary>
        public void GetPeerAccountInfo(int peerId, PeerAccountInfoCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(null, "Not connected to server");
                return;
            }

            Connection.SendMessage((short)OpCodes.GetPeerAccountInfo, peerId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown error"));
                    return;
                }

                var data = response.Deserialize(new PeerAccountInfoPacket());

                callback.Invoke(data, null);
            });
        }
    }
}