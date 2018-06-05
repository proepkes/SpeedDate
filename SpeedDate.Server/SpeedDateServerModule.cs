using System;
using Ninject.Modules;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;

namespace SpeedDate.Server
{
    public sealed class SpeedDateServerModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IServer, ISpeedDateStartable>().To<SpeedDateServer>().InSingletonScope();
        }
    }
}