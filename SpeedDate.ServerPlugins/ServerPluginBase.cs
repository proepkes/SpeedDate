using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.ServerPlugins
{
    public abstract class ServerPluginBase : IPlugin
    {
        [Inject] protected readonly IServer Server;

        public virtual void Loaded(IPluginProvider pluginProvider)
        {
        }
    }
}