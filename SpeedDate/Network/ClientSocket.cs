using System;
using System.Collections.Generic;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;
using SpeedDate.Network.LiteNetLib;

namespace SpeedDate.Network
{
    public class ClientSocket : BaseClientSocket, IClientSocket, IUpdatable
    {
        [Inject] private readonly AppUpdater _appUpdater;
        private readonly EventBasedNetListener _listener = new EventBasedNetListener();
        private readonly NetManager _client;

        private Peer _peer;
        private readonly Dictionary<ushort, IPacketHandler> _handlers;

        public long PeerId => _peer.Id;

        public bool IsConnected => _peer != null && _peer.ConnectionState == ConnectionState.Connected;
        public bool IsConnecting => _peer != null && _peer.ConnectionState == ConnectionState.InProgress;

        public string ConnectionIp { get; private set; }

        public int ConnectionPort { get; private set; }

        public event Action Connected;
        public event Action Disconnected;

        public ClientSocket(AppUpdater appUpdater)
        {
            _appUpdater = appUpdater;

            _handlers = new Dictionary<ushort, IPacketHandler>();

            _listener.PeerConnectedEvent += peer =>
            {
                Connected?.Invoke();
            };
            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                Disconnected?.Invoke();
                _peer.NotifyDisconnectEvent();
            };
            _listener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                _peer.HandleDataReceived(reader.Data, 0);
            };
            _client = new NetManager(_listener);
        }

        public IClientSocket Connect(string ip, int port)
        {
            _appUpdater.Add(this);
            
            ConnectionIp = ip;
            ConnectionPort = port;
            _client.Start();

            _peer = new Peer(_client.Connect(ip, port, "TundraNet"), _appUpdater);
            _peer.MessageReceived += HandleMessage;
            Peer = _peer;
            return this;
        }

        public void WaitForConnection(Action<IClientSocket> connectionCallback, float timeoutSeconds)
        {
            if (IsConnected)
            {
                connectionCallback.Invoke(this);
                return;
            }

            var isConnected = false;
            var timedOut = false;

            void OnConnected()
            {
                Connected -= OnConnected;
                isConnected = true;

                if (!timedOut)
                {
                    connectionCallback.Invoke(this);
                }
            }

            Connected += OnConnected;

            AppTimer.AfterSeconds(timeoutSeconds, () =>
            {
                if (!isConnected)
                {
                    timedOut = true;
                    Connected -= OnConnected;
                    connectionCallback.Invoke(this);
                }
            });
        }

        public void WaitForConnection(Action<IClientSocket> connectionCallback)
        {
            WaitForConnection(connectionCallback, 10);
        }

        public void AddConnectionListener(Action callback, bool invokeInstantlyIfConnected = true)
        {
            Connected += callback;

            if (IsConnected && invokeInstantlyIfConnected)
                callback.Invoke();
        }

        public void RemoveConnectionListener(Action callback)
        {
            Connected -= callback;
        }

        public IPacketHandler SetHandler(IPacketHandler handler)
        {
            _handlers[handler.OpCode] = handler;
            return handler;
        }

        public IPacketHandler SetHandler(ushort opCode, IncommingMessageHandler handlerMethod)
        {
            var handler = new PacketHandler(opCode, handlerMethod);
            return SetHandler(handler);
        }

        public void RemoveHandler(IPacketHandler handler)
        {
            _handlers.TryGetValue(handler.OpCode, out var previousHandler);

            if (previousHandler != handler)
                return;

            _handlers.Remove(handler.OpCode);
        }

        public void Reconnect()
        {
            Disconnect();
            Connect(ConnectionIp, ConnectionPort);
        }

        public void Disconnect()
        {
            _peer.Disconnect("");
            _peer.MessageReceived -= HandleMessage;
        }

        public void Update()
        {
            if (_peer == null)
            {
                return;
            }

            _client.PollEvents();
        }

        private void HandleMessage(IIncommingMessage message)
        {
            try
            {
                _handlers.TryGetValue(message.OpCode, out var handler);

                if (handler != null)
                    handler.Handle(message);
                else if (message.IsExpectingResponse)
                {
                    Logs.Error("Connection is missing a handler. OpCode: " + message.OpCode);
                    message.Respond(ResponseStatus.Error);
                }
            }
            catch (Exception e)
            {
                Logs.Error("Failed to handle a message. OpCode: " + message.OpCode);
                Logs.Error(e);

                if (!message.IsExpectingResponse)
                    return;

                try
                {
                    message.Respond(ResponseStatus.Error);
                }
                catch (Exception exception)
                {
                    Logs.Error(exception);
                }
            }
        }
    }
}
