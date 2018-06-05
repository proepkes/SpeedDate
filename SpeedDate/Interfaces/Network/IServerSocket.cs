namespace SpeedDate.Interfaces.Network
{
    public delegate void PeerActionHandler(IPeer peer);

    public interface IServerSocket
    {
        /// <summary>
        /// Invoked, when a client connects to this socket
        /// </summary>
        event PeerActionHandler Connected;
        
        /// <summary>
        /// Invoked, when client disconnects from this socket
        /// </summary>
        event PeerActionHandler Disconnected;

        /// <summary>
        /// Opens the socket and starts listening to a given port
        /// </summary>
        /// <param name="port"></param>
        bool Listen(int port);

        /// <summary>
        /// Stops listening
        /// </summary>
        void Stop();
    }
}