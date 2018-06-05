using Ninject.Modules;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;

namespace SpeedDate.Client
{
    public class SpeedDateClientModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IClient, ISpeedDateListener>().To<SpeedDateClient>();
        }
    }
}
