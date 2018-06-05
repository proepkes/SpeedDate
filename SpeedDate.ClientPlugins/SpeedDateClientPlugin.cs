using System;
using System.Collections.Generic;
using System.Text;
using SpeedDate.Interfaces;
using SpeedDate.Networking;
using SpeedDate.Plugin;

namespace SpeedDate.ClientPlugins
{
    public abstract class SpeedDateClientPlugin : IPlugin
    {
        protected readonly IClientSocket Connection;

        public SpeedDateClientPlugin(IClientSocket connection)
        {
            Connection = connection;
        }
        public void Loaded(IPluginProvider pluginProvider)
        {
            
        }
    }
}
