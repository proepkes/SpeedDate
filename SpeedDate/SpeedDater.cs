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
    public sealed class SpeedDater
    {
        private SpeedDateConfig _config;
        private TinyIoCContainer _kernel;

        public event Action Started;
        public event Action Stopped;

        public bool IsStarted { get; set; }

        public IPluginProvider PluginProver
        {
            get;
            private set;
        }


        public void Start(IConfigProvider configProvider)
        {
            var logger = LogManager.GetLogger("SpeedDate");

            _kernel = CreateKernel();
            
            _config = configProvider.Create(_kernel.ResolveAll<IConfig>());

            var startable = _kernel.Resolve<ISpeedDateStartable>();
            _kernel.BuildUp(startable);
            startable.Started += () =>
            {
                IsStarted = true;
                Started?.Invoke();
            };
            startable.Stopped += () =>
            {
                IsStarted = false;
                Stopped?.Invoke();
            };

            PluginProver = _kernel.Resolve<IPluginProvider>();

            foreach (var plugin in _kernel.ResolveAll<IPlugin>())
            {
                if (_config.Plugins.LoadAll || _config.Plugins.PluginsNamespaces.Split(';').Any(ns => Regex.IsMatch(plugin.GetType().Namespace, WildCardToRegular(ns))))
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
                    _kernel.BuildUp(plugin);


                    PluginProver.RegisterPlugin(plugin);
                }
            }

            foreach (var plugin in PluginProver.GetAll())
            {
                plugin.Loaded(PluginProver);
                logger.Info($"Loaded {plugin.GetType().Name}");
            }

            startable.Start(_config.Network);
        }

        public void Stop()
        {
            _kernel.TryResolve<ISpeedDateStartable>(out var startable);
            startable.Stop();
        }

        private static TinyIoCContainer CreateKernel()
        {
            try
            {
                //Register possible plugin-dependencies
                TinyIoCContainer.Current.Register<IClientSocket, ClientSocket>();
                TinyIoCContainer.Current.Register<IServerSocket, ServerSocket>();
                TinyIoCContainer.Current.Register<IPluginProvider, PluginProvider>();
                TinyIoCContainer.Current.Register<ILogger>((container, overloads, requestType) => LogManager.GetLogger(requestType.Name));

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

                    foreach (var startableType in assembly.DefinedTypes.Where(info =>
                        !info.IsAbstract && !info.IsInterface && typeof(ISpeedDateStartable).IsAssignableFrom(info)))
                    {
                        var startableInstance = (ISpeedDateStartable)Activator.CreateInstance(startableType);

                        switch (startableInstance)
                        {
                            case IServer _:
                                TinyIoCContainer.Current.Register((container, overloads, requesttype) =>
                                    (IServer) startableInstance);
                                break;
                            case IClient _:
                                TinyIoCContainer.Current.Register((container, overloads, requesttype) =>
                                    (IClient) startableInstance);
                                break;
                        }
                        
                        TinyIoCContainer.Current.Register(startableInstance);
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
