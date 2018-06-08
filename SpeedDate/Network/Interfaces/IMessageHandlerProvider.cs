namespace SpeedDate.Network.Interfaces
{
    public interface IMessageHandlerProvider
    {
        void SetHandler(short opCode, IncommingMessageHandler handler);
    }
}