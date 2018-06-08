using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Plugins;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.ServerPlugins
{
    public abstract class ServerPluginBase : IPlugin
    {
        protected readonly IServer Server;

        protected ServerPluginBase(IServer server)
        {
            Server = server;
        }


        public virtual void Loaded(IPluginProvider pluginProvider)
        {
        }
    }
}