using System;
using System.Collections.Generic;
using System.Text;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.ClientPlugins
{
    public abstract class SpeedDateClientPlugin : IPlugin
    {
        [Inject]
        protected readonly IClient Client;

        public virtual void Loaded(IPluginProvider pluginProvider)
        {
            
        }
    }
}
