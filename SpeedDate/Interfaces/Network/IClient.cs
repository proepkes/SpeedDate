namespace SpeedDate.Interfaces
{
    public interface IClient
    {
        IClientSocket Connection { get; }
    }
}