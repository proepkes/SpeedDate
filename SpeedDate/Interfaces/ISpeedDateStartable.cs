using System;

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