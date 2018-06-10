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
            container.Register<ISpeedDateStartable, SpeedDateServer>();
            container.Register((c, p, t) => c.Resolve<ISpeedDateStartable>() as IServer);
        }
    }
}
