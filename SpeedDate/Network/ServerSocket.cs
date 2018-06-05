using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.LiteNetLib;
using SpeedDate.Logging;

namespace SpeedDate.Networking
{
    public class ServerSocket : IServerSocket, IUpdatable
    {
        private readonly Dictionary<long, Peer> _connections = new Dictionary<long, Peer>(500);
        readonly EventBasedNetListener _listener = new EventBasedNetListener();
        private readonly NetManager _server;

        public ServerSocket()
        {
            _server = new NetManager(_listener, 5000 /* maximum clients */);
            
        }
        public event PeerActionHandler Connected;
        public event PeerActionHandler Disconnected;
        public bool Listen(int port)
        {
            _listener.ConnectionRequestEvent += request => request.AcceptIfKey("TundraNet");
            _listener.PeerConnectedEvent += peer =>
            {
                var client = new Peer(peer);
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
                _connections.Remove(peer.ConnectId);
            };

            AppUpdater.Instance.Add(this);
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
