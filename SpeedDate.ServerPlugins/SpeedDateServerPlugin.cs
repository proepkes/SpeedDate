using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.ServerPlugins
{
    public abstract class SpeedDateServerPlugin : IPlugin
    {
        [Inject] protected readonly IServer Server;

        public virtual void Loaded(IPluginProvider pluginProvider)
        {
        }
    }
}