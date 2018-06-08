namespace SpeedDate.Network.Interfaces
{
    public interface IClient
    {
        IClientSocket Connection { get; }
    }
}