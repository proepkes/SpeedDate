using System;
using System.Collections.Generic;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Network.LiteNetLib;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.Client
{
    public sealed class SpeedDateClient : IClient, ISpeedDateStartable, IDisposable, IUpdatable
    {
        private readonly Dictionary<ushort, IPacketHandler> _handlers;
        private readonly SpeedDateKernel _kernel;
        private readonly SpeedDateNetListener _listener;

        [Inject] private ILogger _logger;
        private NetPeer _netPeer;
        private NetManager _manager;

        private float _timeToConnect = 0.5f;
        public SpeedDateConfig Config { get; private set; }

        public event Action Started;
        public event Action Stopped;

        public SpeedDateClient()
        {
            _handlers = new Dictionary<ushort, IPacketHandler>();
            _kernel = new SpeedDateKernel();
            _listener = new SpeedDateNetListener();
            
            _listener.NetworkErrorEvent += (point, code) =>
            {
                Logs.Error($"NetworkError: ({code}): {point}");
            };
            _listener.PeerConnectedEvent += peer =>
            {
                _netPeer.MessageReceived += HandleMessage;

                _logger.Info("Connected");
                Started?.Invoke();
            };
            _listener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                _netPeer.HandleDataReceived(reader.Data);
            };
            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                _manager.Stop();

                AppUpdater.Instance.Remove(this);
                
                _logger.Info("Disconnected");
                Stopped?.Invoke();
            };

            _manager = new NetManager(_listener);
        }


        public bool IsConnected => _netPeer != null && _netPeer.ConnectionState == ConnectionState.Connected;

        public void Reconnect()
        {
            void StartAfterStop()
            {
                Stopped -= StartAfterStop;
                
                AppUpdater.Instance.Add(this);
                _manager.Start();
                _netPeer = _manager.Connect(Config.Network.Address, Config.Network.Port, "TundraNet");
            }

            Stopped += StartAfterStop;
            Stop();
        }

        public void SetHandler(ushort opCode, IncommingMessageHandler handler)
        {
            SetHandler(new PacketHandler(opCode, handler));
        }

        public void SetHandler(OpCodes opCode, IncommingMessageHandler handler)
        {
            SetHandler(new PacketHandler((ushort) opCode, handler));
        }

        public void SendMessage(ushort opCode)
        {
            _netPeer.SendMessage(opCode);
        }

        public void SendMessage(ushort opCode, ResponseCallback responseCallback)
        {
            _netPeer.SendMessage(opCode, responseCallback);
        }

        public void SendMessage(ushort opCode, ISerializablePacket packet)
        {
            _netPeer.SendMessage(opCode, packet);
        }

        public void SendMessage(ushort opCode, ISerializablePacket packet, DeliveryMethod method)
        {
            _netPeer.SendMessage(opCode, packet, method);
        }

        public void SendMessage(ushort opCode, ISerializablePacket packet, ResponseCallback responseCallback)
        {
            _netPeer.SendMessage(opCode, packet, responseCallback);
        }

        public void SendMessage(ushort opCode, byte[] data)
        {
            _netPeer.SendMessage(opCode, data);
        }

        public void SendMessage(ushort opCode, byte[] data, DeliveryMethod method)
        {
            _netPeer.SendMessage(opCode, data, method);
        }

        public void SendMessage(ushort opCode, byte[] data, ResponseCallback responseCallback)
        {
            _netPeer.SendMessage(opCode, data, responseCallback);
        }

        public void SendMessage(ushort opCode, string data)
        {
            _netPeer.SendMessage(opCode, data);
        }

        public void SendMessage(ushort opCode, string data, DeliveryMethod method)
        {
            _netPeer.SendMessage(opCode, data, method);
        }

        public void SendMessage(ushort opCode, string data, ResponseCallback responseCallback)
        {
            _netPeer.SendMessage(opCode, data, responseCallback);
        }

        public void SendMessage(ushort opCode, int data)
        {
            _netPeer.SendMessage(opCode, data);
        }

        public void SendMessage(ushort opCode, int data, DeliveryMethod method)
        {
            _netPeer.SendMessage(opCode, data, method);
        }

        public void SendMessage(ushort opCode, int data, ResponseCallback responseCallback)
        {
            _netPeer.SendMessage(opCode, data, responseCallback);
        }

        public void SendMessage(ushort opCode, bool data)
        {
            _netPeer.SendMessage(opCode, data);
        }

        public void SendMessage(ushort opCode, bool data, DeliveryMethod method)
        {
            _netPeer.SendMessage(opCode, data, method);
        }

        public void SendMessage(ushort opCode, bool data, ResponseCallback responseCallback)
        {
            _netPeer.SendMessage(opCode, data, responseCallback);
        }

        public void SendMessage(IMessage message, DeliveryMethod method)
        {
            ((IMsgDispatcher) _netPeer).SendMessage(message, method);
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start(IConfigProvider configProvider)
        {
            AppUpdater.Instance.Add(this);
            _kernel.Load(this, configProvider, config =>
            {
                Config = config;

                _manager.Start();
                _netPeer = _manager.Connect(config.Network.Address, config.Network.Port, "TundraNet");
            });
        }

        public void Stop()
        {
            _manager.DisconnectAll();
        }
        
        public T GetPlugin<T>() where T : class, IPlugin
        {
            return _kernel.GetPlugin<T>();
        }

        private void HandleMessage(IIncommingMessage message)
        {
            try
            {
                _handlers.TryGetValue(message.OpCode, out var handler);

                if (handler != null)
                {
                    handler.Handle(message);
                }
                else if (message.IsExpectingResponse)
                {
                    Logs.Error("Connection is missing a handler. OpCode: " + message.OpCode);
                    message.Respond(ResponseStatus.Error);
                }
            }
            catch (Exception e)
            {
                if (Enum.TryParse(message.OpCode.ToString(), out OpCodes opcode))
                {
                    Logs.Error($"Failed to handle {opcode}: {e}");
                }
                Logs.Error($"Failed to handle{message.OpCode}: {e}");

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

        public void Update()
        {
            _manager?.PollEvents();
        }

        private void SetHandler(IPacketHandler handler)
        {
            _handlers[handler.OpCode] = handler;
        }
    }
}
