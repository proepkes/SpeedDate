using System;
using System.Collections.Generic;
using System.Linq;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Network.LiteNetLib;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.Server
{
    public sealed class SpeedDateServer : IServer, ISpeedDateStartable, IDisposable, IUpdatable
    {
        private const string InternalServerErrorMessage = "Internal Server Error";

        private readonly SpeedDateKernel _kernel;
        private readonly Dictionary<ushort, IPacketHandler> _handlers;

        [Inject] private ILogger _logger;

        private readonly NetManager _manager;
        private SpeedDateNetListener _listener;

        public event Action Started;
        public event Action Stopped;
        public event PeerActionHandler PeerConnected;
        public event PeerActionHandler PeerDisconnected;


        public SpeedDateServer()
        {
            _kernel = new SpeedDateKernel();
            _handlers = new Dictionary<ushort, IPacketHandler>();

            _listener = new SpeedDateNetListener();
            _listener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey("TundraNet");
            };
            
            _listener.PeerConnectedEvent += peer =>
            {
                // Listen to messages
                peer.MessageReceived += OnMessageReceived;

                // Create the security extension
                var extension = peer.AddExtension(new PeerSecurityExtension());

                // Set default permission level
                extension.PermissionLevel = 0;

                _logger.Info($"Client {peer.ConnectId} connected.");

                PeerConnected?.Invoke(peer);
            };
            
            _listener.NetworkErrorEvent += (point, code) =>
            {
                Logs.Error($"NetworkError: ({code}): {point}");
            };
            
            _listener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                peer.HandleDataReceived(reader.Data);
            };
            
            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                _logger.Info($"Client {peer.ConnectId} disconnected.");

                // Remove listener to messages
                peer.MessageReceived -= OnMessageReceived;

                // Invoke the event
                PeerDisconnected?.Invoke(peer);
            };

            _manager = new NetManager(_listener)
            {
                ReuseAddress = true
            };
        }

        public void Start(IConfigProvider configProvider)
        {
            _kernel.Load(this, configProvider, config =>
            {
                AppUpdater.Instance.Add(this);
                if (_manager.Start(config.Network.Port))
                {
                    _logger.Info($"Listening on: {config.Network.Port}");
                    Started?.Invoke();
                }
            });
        }

        public void Stop()
        {
            _manager.Stop();
            _kernel.Stop();

            AppUpdater.Instance.Remove(this);
            
            Stopped?.Invoke();
        }


        public void Dispose()
        {
            Stop();
        }

        public void SetHandler(ushort opCode, IncommingMessageHandler handler)
        {
            _handlers[opCode] = new PacketHandler(opCode, handler);
        }

        public void SetHandler(OpCodes opCode, IncommingMessageHandler handler)
        {
            SetHandler((ushort)opCode, handler);
        }

        public IPeer GetPeer(long peerId)
        {
            return _manager.ConnectedPeerList.FirstOrDefault(netPeer => netPeer.ConnectId.Equals(peerId));
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

        public T GetPlugin<T>() where T : class, IPlugin
        {
            return _kernel.PluginProvider.Get<T>();
        }

        public void Update()
        {
            _manager?.PollEvents();
        }
    }
}
