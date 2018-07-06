using System;
using SpeedDate.Configuration;

namespace SpeedDate.Network.Interfaces
{
    public interface IClient : IMessageHandlerProvider, IMsgDispatcher
    {
        bool IsConnected { get; }
        SpeedDateConfig Config { get; }
        void Reconnect();
    }
}