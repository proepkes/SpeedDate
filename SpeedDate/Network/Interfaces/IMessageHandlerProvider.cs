namespace SpeedDate.Network.Interfaces
{
    public interface IMessageHandlerProvider
    {
        void SetHandler(ushort opCode, IncommingMessageHandler handler);
        void SetHandler(OpCodes opCode, IncommingMessageHandler handler);
    }
}