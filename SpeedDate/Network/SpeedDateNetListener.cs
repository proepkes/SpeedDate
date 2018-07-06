using System;
using System.Collections.Generic;
using System.Net;
using SpeedDate.Network.LiteNetLib;
using SpeedDate.Network.LiteNetLib.Utils;

namespace SpeedDate.Network
{
    public class SpeedDateNetListener : INetEventListener
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
            PeerConnectedEvent?.Invoke(peer);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            lock (_connectionIds)
            {
                if (_connectionIds.Contains(peer.ConnectId))
                    _connectionIds.Remove(peer.ConnectId);
            }

            PeerDisconnectedEvent?.Invoke(peer, disconnectInfo);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, int socketErrorCode)
        {
            NetworkErrorEvent?.Invoke(endPoint, socketErrorCode);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetDataReader reader, DeliveryMethod deliveryMethod)
        {
            NetworkReceiveEvent?.Invoke(peer, reader, deliveryMethod);
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetDataReader reader,
            UnconnectedMessageType messageType)
        {
            NetworkReceiveUnconnectedEvent?.Invoke(remoteEndPoint, reader, messageType);
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            NetworkLatencyUpdateEvent?.Invoke(peer, latency);
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            ConnectionRequestEvent?.Invoke(request);
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
}