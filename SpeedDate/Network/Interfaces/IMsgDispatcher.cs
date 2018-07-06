using SpeedDate.Network.LiteNetLib;

namespace SpeedDate.Network.Interfaces
{
    public interface IMsgDispatcher
    {
        void SendMessage(ushort opCode);
        void SendMessage(ushort opCode, ResponseCallback responseCallback);

        void SendMessage(ushort opCode, ISerializablePacket packet);
        void SendMessage(ushort opCode, ISerializablePacket packet, DeliveryMethod method);
        void SendMessage(ushort opCode, ISerializablePacket packet, ResponseCallback responseCallback);

        void SendMessage(ushort opCode, byte[] data);
        void SendMessage(ushort opCode, byte[] data, DeliveryMethod method);
        void SendMessage(ushort opCode, byte[] data, ResponseCallback responseCallback);
        void SendMessage(ushort opCode, string data);
        void SendMessage(ushort opCode, string data, DeliveryMethod method);
        void SendMessage(ushort opCode, string data, ResponseCallback responseCallback);
        void SendMessage(ushort opCode, int data);
        void SendMessage(ushort opCode, int data, DeliveryMethod method);
        void SendMessage(ushort opCode, int data, ResponseCallback responseCallback);
        
        void SendMessage(IMessage message, DeliveryMethod method);
        void SendMessage(IMessage message, ResponseCallback responseCallback);

    }
}
