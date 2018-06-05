namespace SpeedDate.Interfaces.Network
{
    public interface IMessageHandlerProvider
    {
        void SetHandler(short opCode, IncommingMessageHandler handler);
    }
}