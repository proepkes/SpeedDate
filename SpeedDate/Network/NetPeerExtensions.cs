using System;
using System.Collections.Generic;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;
using SpeedDate.Network.LiteNetLib.Utils;

namespace SpeedDate.Network.LiteNetLib
{
    public sealed partial class NetPeer : IPeer
    {
        private int _nextAckId = 1;

        public const int DefaultTimeout = 60;

        public event PeerActionHandler Disconnected;
        public event Action<IIncommingMessage> MessageReceived;

        private readonly Dictionary<int, object> _data = new Dictionary<int, object>(30);
        private readonly Dictionary<Type, object> _extensions = new Dictionary<Type, object>();

        private readonly List<long[]> _ackTimeoutQueue = new List<long[]>(100);
        private readonly Dictionary<int, ResponseCallback> _acks = new Dictionary<int, ResponseCallback>(30);

        private IIncommingMessage _timeoutMessage;


        private void InitializeAckDisposal()
        {
            _timeoutMessage = new IncommingMessage(OpCodes.Error, 0, "Time out".ToBytes(),
                DeliveryMethod.ReliableUnordered, this)
            {
                Status = ResponseStatus.Timeout
            };

            AppUpdater.Instance.OnTick += HandleAckDisposalTick;
        }

        public T AddExtension<T>(T extension)
        {
            _extensions[typeof(T)] = extension;
            return extension;
        }

        public T GetExtension<T>()
        {
            _extensions.TryGetValue(typeof(T), out var extension);
            if (extension == null)
                return default(T);

            return (T)extension;
        }

        public bool HasExtension<T>()
        {
            return _extensions.ContainsKey(typeof(T));
        }

        public void HandleDataReceived(byte[] buffer)
        {
            IIncommingMessage message;

            try
            {
                message = MessageHelper.FromBytes(buffer, 0, this);

                if (message.AckRequestId.HasValue)
                {
                    // We received a message which is a response to our ack request
                    TriggerAck(message.AckRequestId.Value, message.Status, message);
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed parsing an incomming message: " + e);

                return;
            }

            MessageReceived?.Invoke(message);
        }
        
        public void Disconnect(string reason)
        {
            Disconnect(NetDataWriter.FromString(reason));
        }

        public void SetProperty(int id, object data)
        {
            if (_data.ContainsKey(id))
                _data[id] = data;
            else
                _data.Add(id, data);
        }

        public object GetProperty(int id)
        {
            _data.TryGetValue(id, out var value);

            return value;
        }

        public object GetProperty(int id, object defaultValue)
        {
            var obj = GetProperty(id);
            return obj ?? defaultValue;
        }


        private void SendMessage(IMessage message, DeliveryMethod deliveryMethod)
        {
            Send(message.ToBytes(), deliveryMethod);
        }

        public void SendMessage(IMessage message, ResponseCallback responseCallback)
        {
            SendMessage(message.OpCode, message.ToBytes(), responseCallback);
        }

        private void SendMessage(IMessage message, ResponseCallback responseCallback,
            int timeoutSecs, DeliveryMethod deliveryMethod)
        {
            if (ConnectionState != ConnectionState.Connected)
            {
                responseCallback.Invoke(ResponseStatus.NotConnected, null);
                return;
            }

            RegisterAck(message, responseCallback, timeoutSecs);

            SendMessage(message, deliveryMethod);
        }

        public void SendMessage(ushort opCode)
        {
            SendMessage(MessageHelper.Create(opCode), DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, ResponseCallback responseCallback)
        {
            SendMessage(MessageHelper.Create(opCode), responseCallback, DefaultTimeout, DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, ISerializablePacket packet)
        {
            SendMessage(MessageHelper.Create(opCode, packet), DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, ISerializablePacket packet, DeliveryMethod method)
        {
            SendMessage(MessageHelper.Create(opCode, packet), method);
        }

        public void SendMessage(ushort opCode, ISerializablePacket packet, ResponseCallback responseCallback)
        {
            SendMessage(MessageHelper.Create(opCode, packet), responseCallback, DefaultTimeout, DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, byte[] data)
        {
            SendMessage(MessageHelper.Create(opCode, data), DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, byte[] data, DeliveryMethod method)
        {
            SendMessage(MessageHelper.Create(opCode, data), method);
        }

        public void SendMessage(ushort opCode, byte[] data, ResponseCallback responseCallback)
        {
            SendMessage(MessageHelper.Create(opCode, data), responseCallback, DefaultTimeout, DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, string data)
        {
            SendMessage(MessageHelper.Create(opCode, data), DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, string data, DeliveryMethod method)
        {
            SendMessage(MessageHelper.Create(opCode, data), method);
        }

        public void SendMessage(ushort opCode, string data, ResponseCallback responseCallback)
        {
            SendMessage(MessageHelper.Create(opCode, data), responseCallback, DefaultTimeout, DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, int data)
        {
            SendMessage(MessageHelper.Create(opCode, data), DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, int data, DeliveryMethod method)
        {
            SendMessage(MessageHelper.Create(opCode, data), method);
        }

        public void SendMessage(ushort opCode, int data, ResponseCallback responseCallback)
        {
            SendMessage(MessageHelper.Create(opCode, data), responseCallback, DefaultTimeout, DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, bool data)
        {
            SendMessage(MessageHelper.Create(opCode, data), DeliveryMethod.ReliableUnordered);
        }

        public void SendMessage(ushort opCode, bool data, DeliveryMethod method)
        {
            SendMessage(MessageHelper.Create(opCode, data), method);
        }

        public void SendMessage(ushort opCode, bool data, ResponseCallback responseCallback)
        {
            SendMessage(MessageHelper.Create(opCode, data), responseCallback, DefaultTimeout, DeliveryMethod.ReliableUnordered);
        }

        void IMsgDispatcher.SendMessage(IMessage message,  DeliveryMethod method)
        {
            SendMessage(message, method);
        }
        
        public void NotifyDisconnected()
        {
            Disconnected?.Invoke(this);
        }

        private void RegisterAck(IMessage message, ResponseCallback responseCallback,
            int timeoutSecs)
        {
            int id;

            lock (_acks)
            {
                id = _nextAckId++;
                _acks.Add(id, responseCallback);
            }

            message.AckRequestId = id;

            StartAckTimeout(id, timeoutSecs);
        }
        private void TriggerAck(int ackId, ResponseStatus statusCode, IIncommingMessage message)
        {
            ResponseCallback ackCallback;
            lock (_acks)
            {
                _acks.TryGetValue(ackId, out ackCallback);

                if (ackCallback == null) return;

                _acks.Remove(ackId);
            }

            ackCallback(statusCode, message);
        }

        private void StartAckTimeout(int ackId, int timeoutSecs)
        {
            // +1, because it might be about to tick in a few miliseconds
            _ackTimeoutQueue.Add(new[] { ackId, AppUpdater.Instance.CurrentTick + timeoutSecs + 1 });
        }
        private void HandleAckDisposalTick(long currentTick)
        {
            // TODO test with ordered queue, might be more performant
            _ackTimeoutQueue.RemoveAll(a =>
            {
                if (a[1] > currentTick) return false;

                try
                {
                    CancelAck((int)a[0], ResponseStatus.Timeout);
                }
                catch (Exception e)
                {
                    Logs.Error(e);
                }

                return true;
            });
        }

        private void CancelAck(int ackId, ResponseStatus responseCode)
        {
            ResponseCallback ackCallback;
            lock (_acks)
            {
                _acks.TryGetValue(ackId, out ackCallback);

                if (ackCallback == null) return;

                _acks.Remove(ackId);
            }

            ackCallback(responseCode, _timeoutMessage);
        }
    }
}
