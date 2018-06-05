using Ninject.Modules;
using SpeedDate.Interfaces;

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
