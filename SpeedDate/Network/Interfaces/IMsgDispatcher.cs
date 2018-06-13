using SpeedDate.Network.LiteNetLib;

namespace SpeedDate.Network.Interfaces
{
    public interface IMsgDispatcher
    {

        void SendMessage(ushort opCode);
        void SendMessage(ushort opCode, ISerializablePacket packet);
        void SendMessage(ushort opCode, ISerializablePacket packet, DeliveryMethod method);
        void SendMessage(ushort opCode, ISerializablePacket packet, ResponseCallback responseCallback);
        void SendMessage(ushort opCode, ISerializablePacket packet, ResponseCallback responseCallback, int timeoutSecs);

        void SendMessage(ushort opCode, ResponseCallback responseCallback);

        void SendMessage(ushort opCode, byte[] data);
        void SendMessage(ushort opCode, byte[] data, ResponseCallback responseCallback);
        void SendMessage(ushort opCode, byte[] data, ResponseCallback responseCallback, int timeoutSecs);

        void SendMessage(ushort opCode, string data);
        void SendMessage(ushort opCode, string data, ResponseCallback responseCallback);
        void SendMessage(ushort opCode, string data, ResponseCallback responseCallback, int timeoutSecs);

        void SendMessage(ushort opCode, int data);
        void SendMessage(ushort opCode, int data, ResponseCallback responseCallback);
        void SendMessage(ushort opCode, int data, ResponseCallback responseCallback, int timeoutSecs);

        void SendMessage(IMessage message);
        void SendMessage(IMessage message, ResponseCallback responseCallback);
        void SendMessage(IMessage message, ResponseCallback responseCallback, int timeoutSecs);
    }
}