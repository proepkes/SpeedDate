using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate
{
    public delegate void KernelStartedCallback(SpeedDateConfig config);
    public sealed class SpeedDateKernel
    {
        private SpeedDateConfig _config;
        private TinyIoCContainer _container;
        
        public IPluginProvider PluginProvider
        {
            get;
            private set;
        }

        public void Load(ISpeedDateStartable startable, IConfigProvider configProvider, KernelStartedCallback startedCallback)
        {
            var logger = LogManager.GetLogger("SpeedDate");

            _container = CreateContainer(startable);

            _config = configProvider.Configure(_container.ResolveAll<IConfig>());
            
            _container.BuildUp(startable);

            PluginProvider = _container.Resolve<IPluginProvider>();

            foreach (var plugin in _container.ResolveAll<IPlugin>())
            {
                if (_config.Plugins.Namespaces.Split(';').Any(ns => Regex.IsMatch(plugin.GetType().Namespace, WildCardToRegular(ns))))
                {
                    //Inject configs, cannot use _kernel because the configProvider may have added additional IConfigs
                    var fields = from field in plugin.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                        where !field.FieldType.IsValueType() && Attribute.IsDefined(field, typeof(InjectAttribute))
                        select field;

                    foreach (var field in fields)
                    {
                        if (field.GetValue(plugin) == null && _config.TryGetConfig(field.FieldType.FullName, out var config))
                        {
                            field.SetValue(plugin, config);
                        }
                    }

                    //Inject ILogger & other possible dependencies, Configs are already set above and will not be overwritten
                    _container.BuildUp(plugin);

                    PluginProvider.RegisterPlugin(plugin);
                }
            }

            foreach (var plugin in PluginProvider.GetAll())
            {
                plugin.Loaded(PluginProvider);
                logger.Info($"Loaded {plugin.GetType().Name}");
            }

            startedCallback.Invoke(_config);
        }

        public void Stop()
        {
            AppUpdater.Instance.keepRunning = false;
            AppTimer.Instance.keepRunning = false;
        }

        private static TinyIoCContainer CreateContainer(ISpeedDateStartable startable)
        {
            try
            {
                //Register possible plugin-dependencies
                TinyIoCContainer.Current.Register<IClientSocket, ClientSocket>();
                TinyIoCContainer.Current.Register<IServerSocket, ServerSocket>();
                TinyIoCContainer.Current.Register<IPluginProvider, PluginProvider>();
                TinyIoCContainer.Current.Register<ILogger>((container, overloads, requestType) => LogManager.GetLogger(requestType.Name));

                switch (startable)
                {
                    case IServer _:
                        TinyIoCContainer.Current.Register((container, overloads, requesttype) =>
                            (IServer)startable);
                        break;
                    case IClient _:
                        TinyIoCContainer.Current.Register((container, overloads, requesttype) =>
                            (IClient)startable);
                        break;
                }

                //Register configs & plugins
                foreach (var dllFile in
                    Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "*.dll"))
                {
                    var assembly = Assembly.LoadFrom(dllFile);
                    
                    //Register Configurations
                    foreach (var pluginConfigType in assembly.DefinedTypes.Where(info =>
                        !info.IsAbstract && !info.IsInterface && typeof(IConfig).IsAssignableFrom(info)))
                    {
                        var pluginConfig = (IConfig)Activator.CreateInstance(pluginConfigType);
                        TinyIoCContainer.Current.Register(pluginConfig, pluginConfigType.FullName);
                    }

                    foreach (var pluginType in assembly.DefinedTypes.Where(info =>
                        !info.IsAbstract && !info.IsInterface && typeof(IPlugin).IsAssignableFrom(info)))
                    {
                        var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                        TinyIoCContainer.Current.Register(plugin, pluginType.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return TinyIoCContainer.Current;
        }

        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }
    }
}
