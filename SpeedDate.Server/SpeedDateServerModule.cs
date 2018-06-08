using System;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Interfaces.Plugins;
using TinyIoC;

namespace SpeedDate.Server
{
    public sealed class SpeedDateServerModule : ISpeedDateModule
    {
        public void Load(TinyIoCContainer container)
        {
            container.Register<IServer, SpeedDateServer>();
            container.Register<ISpeedDateStartable, SpeedDateServer>();
        }
    }
}
