
using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Authentication;

namespace SpeedDate.ClientPlugins.GameServer
{
    public class PeerInfoPlugin : SpeedDateClientPlugin
    {
        public delegate void PeerAccountInfoCallback(PeerAccountInfoPacket info);
        
        /// <summary>
        /// Gets account information of a client, who is connected to master server, 
        /// and who's peer id matches the one provided
        /// </summary>
        public void GetPeerAccountInfo(int peerId, PeerAccountInfoCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected to server");
                return;
            }

            Client.SendMessage((ushort)OpCodes.GetPeerAccountInfo, peerId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                var data = response.Deserialize<PeerAccountInfoPacket>();

                callback.Invoke(data);
            });
        }
    }
}
