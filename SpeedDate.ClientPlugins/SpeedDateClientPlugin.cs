using System;
using System.Collections.Generic;
using System.Text;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Interfaces.Plugins;

namespace SpeedDate.ClientPlugins
{
    public abstract class SpeedDateClientPlugin : IPlugin
    {
        protected readonly IClientSocket Connection;

        protected SpeedDateClientPlugin(IClientSocket connection)
        {
            Connection = connection;
        }
        public virtual void Loaded(IPluginProvider pluginProvider)
        {
            
        }
    }
}
