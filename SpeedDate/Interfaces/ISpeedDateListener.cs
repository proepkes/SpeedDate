using System;

namespace SpeedDate.Interfaces
{
    public interface ISpeedDateListener
    {
        void OnSpeedDateStarted();
        void OnSpeedDateStopped();
    }
}