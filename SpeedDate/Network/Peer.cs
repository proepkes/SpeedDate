using SpeedDate.Interfaces;
using SpeedDate.LiteNetLib;
using SpeedDate.LiteNetLib.Utils;

namespace SpeedDate.Networking
{
    class Peer : BasePeer
    {
        public override long Id => _socket.ConnectId;
        public override bool IsConnected => _socket.ConnectionState == ConnectionState.Connected;

        private readonly NetPeer _socket;

        public ConnectionState ConnectionState => _socket.ConnectionState;

        public Peer(NetPeer socket)
        {
            _socket = socket;
        }

        public override void SendMessage(IMessage message, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableUnordered)
        {
            _socket.Send(message.ToBytes(), deliveryMethod);
        }

        public override void Disconnect(string reason)
        {
            _socket.Disconnect(NetDataWriter.FromString(reason));
        }
    }
}
