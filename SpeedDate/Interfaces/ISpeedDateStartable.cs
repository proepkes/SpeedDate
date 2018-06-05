using System;
using SpeedDate.Interfaces.Plugins;

namespace SpeedDate.Interfaces
{
    public interface ISpeedDateStartable
    {
        event Action Started;
        event Action Stopped;

        void Start();
        void Stop();
    }
}