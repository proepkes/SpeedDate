﻿using System.Linq;
using System.Reflection;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.ClientPlugins.Spawner
{
    public class SpawnerPluginsModule : ISpeedDateModule
    {
        public void Load(TinyIoCContainer container)
        {
            //Register all types that implement IPlugin
            container.RegisterMultiple<IPlugin>(Assembly.GetExecutingAssembly().DefinedTypes.Where(info =>
                !info.IsAbstract && !info.IsInterface && typeof(IPlugin).IsAssignableFrom(info)));
        }
    }
}
