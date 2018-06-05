using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Interfaces.Plugins;

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