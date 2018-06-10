using System;
using System.Linq;
using System.Reflection;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.ServerPlugins.Lobbies;
using SpeedDate.ServerPlugins.Rooms;

namespace SpeedDate.ServerPlugins
{
    public class ServerPluginsModule : ISpeedDateModule
    {
        public void Load(TinyIoCContainer container)
        {
            //Register all types that implement IPlugin
            container.RegisterMultiple<IPlugin>(Assembly.GetExecutingAssembly().DefinedTypes.Where(info =>
                !info.IsAbstract && !info.IsInterface && typeof(IPlugin).IsAssignableFrom(info)));
        }
    }
}
