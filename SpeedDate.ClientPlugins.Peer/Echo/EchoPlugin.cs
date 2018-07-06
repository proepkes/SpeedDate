using System;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;

namespace SpeedDate.ClientPlugins.Peer.Echo
{
    public class EchoPlugin : SpeedDateClientPlugin
    {
        public void Send(string message, Action<string> echoCallback, ErrorCallback error)
        {
            Client.SendMessage((ushort)OpCodes.Echo, message, (status, response) => 
            { 
                if (status != ResponseStatus.Success)
                {
                    error.Invoke(response.AsString("Unknown error"));
                }
                
                echoCallback.Invoke(response.AsString());
            });
        }
    }
}
