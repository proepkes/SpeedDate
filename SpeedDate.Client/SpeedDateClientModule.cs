
using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.Client
{
    public class SpeedDateClientModule : ISpeedDateModule
    {
        public void Load(TinyIoCContainer container)
        {
            container.Register<ISpeedDateStartable, SpeedDateClient>();
            container.Register((c, p, t) => c.Resolve<ISpeedDateStartable>() as IClient);
        }
    }
}
