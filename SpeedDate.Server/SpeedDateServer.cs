using System;
using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;

namespace SpeedDate.Server
{
    internal sealed class SpeedDateServer : IServer, ISpeedDateStartable, IDisposable
    {
        private const string InternalServerErrorMessage = "Internal Server Error";
        private readonly Dictionary<long, IPeer> _connectedPeers;
        private readonly Dictionary<short, IPacketHandler> _handlers;
        private readonly ILogger _logger;

        private readonly IServerSocket _socket;


        public SpeedDateServer(IServerSocket serverSocket, ILogger logger)
        {
            _connectedPeers = new Dictionary<long, IPeer>();
            _handlers = new Dictionary<short, IPacketHandler>();

            // Create the server 
            _socket = serverSocket;
            _logger = logger;

            _socket.Connected += Connected;
            _socket.Disconnected += Disconnected;
        }

        public void Dispose()
        {
            _socket.Connected -= Connected;
            _socket.Disconnected -= Disconnected;
        }

        public event PeerActionHandler PeerConnected;
        public event PeerActionHandler PeerDisconnected;

        public void SetHandler(short opCode, IncommingMessageHandler handler)
        {
            _handlers[opCode] = new PacketHandler(opCode, handler);
        }

        public IPeer GetPeer(long peerId)
        {
            _connectedPeers.TryGetValue(peerId, out var peer);
            return peer;
        }

        public event Action Started;
        public event Action Stopped;

        public void Start()
        {
            if (_socket.Listen(SpeedDateConfig.Network.Port))
            {
                _logger.Info("Started on port: " + SpeedDateConfig.Network.Port);
                Started?.Invoke();
            }
        }

        public void Stop()
        {
            _socket.Stop();
            Stopped?.Invoke();
        }

        private void Connected(IPeer peer)
        {
            // Listen to messages
            peer.MessageReceived += OnMessageReceived;

            // Save the peer
            _connectedPeers[peer.Id] = peer;

            // Create the security extension
            var extension = peer.AddExtension(new PeerSecurityExtension());

            // Set default permission level
            extension.PermissionLevel = 0;

            _logger.Info($"New Peer connected. ID: {peer.Id}");
            // Invoke the event
            PeerConnected?.Invoke(peer);
        }

        private void Disconnected(IPeer peer)
        {
            // Remove listener to messages
            peer.MessageReceived -= OnMessageReceived;

            // Remove the peer
            _connectedPeers.Remove(peer.Id);

            // Invoke the event
            PeerDisconnected?.Invoke(peer);
        }

        private void OnMessageReceived(IIncommingMessage message)
        {
            try
            {
                _handlers.TryGetValue(message.OpCode, out var handler);

                if (handler == null)
                {
                    _logger.Warn($"Handler for OpCode {message.OpCode} = {(OpCodes) message.OpCode} does not exist");

                    if (message.IsExpectingResponse)
                    {
                        message.Respond(InternalServerErrorMessage, ResponseStatus.NotHandled);
                        return;
                    }

                    return;
                }

                handler.Handle(message);
            }
            catch (Exception e)
            {
                _logger.Error(
                    $"Error while handling a message from Client. OpCode: {message.OpCode} = {(OpCodes) message.OpCode}");
                _logger.Error(e);

                if (!message.IsExpectingResponse)
                    return;

                try
                {
                    message.Respond(InternalServerErrorMessage, ResponseStatus.Error);
                }
                catch (Exception exception)
                {
                    Logs.Error(exception);
                }
            }
        }
    }
}