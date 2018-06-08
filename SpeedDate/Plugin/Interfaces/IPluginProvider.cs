using System.Collections.Generic;
using SpeedDate.Interfaces.Plugins;

namespace SpeedDate.Plugin.Interfaces
{
    public interface IPluginProvider
    {
        void RegisterPlugin(IPlugin plugin);

        T Get<T>() where T: class, IPlugin;

        IEnumerable<IPlugin> GetAll();

        void Clear();
    }
}