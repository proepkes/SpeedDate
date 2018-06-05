namespace SpeedDate.Interfaces.Network
{
    public interface IClient
    {
        IClientSocket Connection { get; }
    }
}