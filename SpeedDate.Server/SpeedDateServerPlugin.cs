using SpeedDate.Configuration;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.Server
{
    public abstract class SpeedDateServerPlugin : IPlugin
    {
        [Inject] protected readonly IServer Server;

        public virtual void Loaded()
        {
        }
    }
}
