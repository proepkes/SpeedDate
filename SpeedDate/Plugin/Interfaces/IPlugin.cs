
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.Interfaces.Plugins
{
    public interface IPlugin
    {
        /// <summary>
        /// Gets called when all Plugins have been initialized
        /// </summary>
        void Loaded(IPluginProvider pluginProvider);
    }
}