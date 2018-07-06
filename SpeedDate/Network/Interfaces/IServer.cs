namespace SpeedDate.Network.Interfaces
{
    public interface IServer : IMessageHandlerProvider
    {
        /// <summary>
        /// Returns a connected peer with a given ID
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        IPeer GetPeer(long peerId);
    }
}
