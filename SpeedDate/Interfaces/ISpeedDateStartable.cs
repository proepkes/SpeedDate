using System;
using SpeedDate.Configuration;

namespace SpeedDate.Interfaces
{
    public interface ISpeedDateStartable
    {
        event Action Started;
        event Action Stopped;

        void Start(IConfigProvider configProvider);
        void Stop();
    }
}