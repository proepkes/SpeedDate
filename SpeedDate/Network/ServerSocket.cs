using System.Collections.Generic;
using System.Net;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;
using SpeedDate.Network.LiteNetLib;

namespace SpeedDate.Network
{
    public class ServerSocket : IServerSocket, IUpdatable
    {
        [Inject] private readonly AppUpdater _updater;
        private readonly Dictionary<long, Peer> _connections = new Dictionary<long, Peer>(500);
        readonly EventBasedNetListener _listener = new EventBasedNetListener();
        private readonly NetManager _server;

        public ServerSocket()
        {
            _server = new NetManager(_listener)
            {
                ReuseAddress = true,
            };
        }
        public event PeerActionHandler Connected;
        public event PeerActionHandler Disconnected;
        public bool Listen(int port)
        {
            _listener.ConnectionRequestEvent += request => request.AcceptIfKey("TundraNet");
            _listener.PeerConnectedEvent += peer =>
            {
                var client = new Peer(peer, _updater);
                _connections.Add(peer.ConnectId, client);

                Connected?.Invoke(client);
            };
            _listener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                _connections[peer.ConnectId].HandleDataReceived(reader.Data, 0);
            };
            _listener.NetworkErrorEvent += (point, code) =>
            {
                Logs.Error($"NetworkError: ({code}): {point}");
            };
            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Disconnected?.Invoke(_connections[peer.ConnectId]);
                _connections[peer.ConnectId].NotifyDisconnectEvent();
                _connections.Remove(peer.ConnectId);
            };

            _updater.Add(this);
            return _server.Start(port);
        }

        public void Stop()
        {
            _server.Stop();
        }

        public void Update()
        {
            _server.PollEvents();
        }
    }
}
