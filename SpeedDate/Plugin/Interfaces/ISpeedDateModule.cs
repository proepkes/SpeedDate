using System;

namespace SpeedDate.Plugin.Interfaces
{
    public interface ISpeedDateModule
    {
        void Load(TinyIoCContainer container);
    }
}