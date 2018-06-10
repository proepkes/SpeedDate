using System;
using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;

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
