using System.Collections.Generic;
using System.Linq;
using Ninject.Syntax;

namespace SpeedDate.Plugin
{
    class NinjectPluginProvier : IPluginProvider
    {
        private readonly List<IPlugin> _loadedPlugins = new List<IPlugin>();

        public void RegisterPlugin(IPlugin plugin)
        {
            _loadedPlugins.Add(plugin);
        }

        public T Get<T>() where T : class, IPlugin
        {
            return (T) _loadedPlugins.FirstOrDefault(plugin => plugin is T);
        }

        public IEnumerable<IPlugin> GetAll()
        {
            return _loadedPlugins;
        }

        public void Clear()
        {
            _loadedPlugins.Clear();
        }
    }
}