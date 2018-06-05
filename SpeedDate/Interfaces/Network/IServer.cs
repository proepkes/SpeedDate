using System;
using Ninject;

namespace SpeedDate.Interfaces
{
    public interface IServer : IMessageHandlerProvider
    {
        event Action<int> Started;
        event Action Stopped;

        event PeerActionHandler PeerConnected;
        event PeerActionHandler PeerDisconnected;

        /// <summary>
        /// Returns a connected peer with a given ID
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        IPeer GetPeer(long peerId);
    }
}