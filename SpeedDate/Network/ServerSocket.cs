using System;
using System.Collections.Generic;
using System.Net;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;
using SpeedDate.Network.LiteNetLib;
using SpeedDate.Network.LiteNetLib.Utils;

namespace SpeedDate.Network
{
    internal class SpeedDateNetListener : INetEventListener
    {
        public delegate void OnConnectionRequest(ConnectionRequest request);

        public delegate void OnNetworkError(IPEndPoint endPoint, int socketErrorCode);

        public delegate void OnNetworkLatencyUpdate(NetPeer peer, int latency);

        public delegate void OnNetworkReceive(NetPeer peer, NetDataReader reader, DeliveryMethod deliveryMethod);

        public delegate void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetDataReader reader,
            UnconnectedMessageType messageType);

        public delegate void OnPeerConnected(NetPeer peer);

        public delegate void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);

        private readonly HashSet<long> _connectionIds = new HashSet<long>();
        private readonly Random _rnd = new Random((int) DateTime.Now.Ticks);

        public long ValidateConnectionId(long connectionId)
        {
            var newConnectionId = LongRandom(0, long.MaxValue, _rnd);;
            lock (_connectionIds)
            {
                while (_connectionIds.Contains(newConnectionId)) newConnectionId = LongRandom(0, long.MaxValue, _rnd);

                _connectionIds.Add(newConnectionId);
            }

            return newConnectionId;
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            if (PeerConnectedEvent != null)
                PeerConnectedEvent(peer);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            lock (_connectionIds)
            {
                if (_connectionIds.Contains(peer.ConnectId))
                    _connectionIds.Remove(peer.ConnectId);
            }
            
            if (PeerDisconnectedEvent != null)
                PeerDisconnectedEvent(peer, disconnectInfo);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, int socketErrorCode)
        {
            if (NetworkErrorEvent != null)
                NetworkErrorEvent(endPoint, socketErrorCode);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetDataReader reader, DeliveryMethod deliveryMethod)
        {
            if (NetworkReceiveEvent != null)
                NetworkReceiveEvent(peer, reader, deliveryMethod);
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetDataReader reader,
            UnconnectedMessageType messageType)
        {
            if (NetworkReceiveUnconnectedEvent != null)
                NetworkReceiveUnconnectedEvent(remoteEndPoint, reader, messageType);
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            if (NetworkLatencyUpdateEvent != null)
                NetworkLatencyUpdateEvent(peer, latency);
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            if (ConnectionRequestEvent != null)
                ConnectionRequestEvent(request);
        }

        public event OnPeerConnected PeerConnectedEvent;
        public event OnPeerDisconnected PeerDisconnectedEvent;
        public event OnNetworkError NetworkErrorEvent;
        public event OnNetworkReceive NetworkReceiveEvent;
        public event OnNetworkReceiveUnconnected NetworkReceiveUnconnectedEvent;
        public event OnNetworkLatencyUpdate NetworkLatencyUpdateEvent;
        public event OnConnectionRequest ConnectionRequestEvent;


        private long LongRandom(long min, long max, Random rand)
        {
            var buf = new byte[8];
            rand.NextBytes(buf);
            var longRand = BitConverter.ToInt64(buf, 0);

            return Math.Abs(longRand % (max - min)) + min;
        }
    }

    public class ServerSocket : IServerSocket, IUpdatable
    {
        private readonly Dictionary<long, Peer> _connections = new Dictionary<long, Peer>(500);
        private readonly SpeedDateNetListener _listener = new SpeedDateNetListener();
        private readonly NetManager _server;
        [Inject] private readonly AppUpdater _updater;

        public ServerSocket()
        {
            _server = new NetManager(_listener)
            {
                ReuseAddress = true
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
            _listener.NetworkErrorEvent += (point, code) => { Logs.Error($"NetworkError: ({code}): {point}"); };
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
