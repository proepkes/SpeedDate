using SpeedDate.Interfaces;
using SpeedDate.Plugin;

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