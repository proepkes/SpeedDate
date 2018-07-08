using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public void Load(ISpeedDateStartable startable, IConfigProvider configProvider, KernelStartedCallback startedCallback)
        {
            var logger = LogManager.GetLogger("SpeedDate");
            
            _container = CreateContainer(startable);
            
            _config = configProvider.Configure(_container.ResolveAll<IConfig>());
            
            _container.BuildUp(startable);
            
            //Filter plugins for namespace & inject configuration into valid plugins
            foreach (var plugin in _container.ResolveAll<IPlugin>())
            {
                if(_config.Plugins.Namespaces.Split(';').Any(ns => Regex.IsMatch(plugin.GetType().Namespace, WildCardToRegular(ns.Trim()))))
                {
                    logger.Debug($"Loading plugin: {plugin}");
                    //Inject configs, cannot use _container.BuildUp because the configProvider may have additional IConfigs
                    var fields = from field in plugin.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                        where Attribute.IsDefined(field, typeof(InjectAttribute))
                        select field;

                    foreach (var field in fields)
                    {
                        if (field.GetValue(plugin) == null && _config.TryGetConfig(field.FieldType.FullName, out var config))
                        {
                            field.SetValue(plugin, config);
                        }
                    }
                }
                else
                {
                    _container.Unregister(typeof(IPlugin), plugin.GetType().FullName);
                }
            }
           
            //Inject additional dependencies e.g. ILogger 
            foreach (var plugin in _container.ResolveAll<IPlugin>())
            {
                _container.BuildUp(plugin);
            }
            
            //Finally notify ever plugin that loading finished
            foreach (var plugin in _container.ResolveAll<IPlugin>())
            {
                plugin.Loaded();
                logger.Info($"Loaded {plugin.GetType().Name}");
            }

            startedCallback.Invoke(_config);
        }

        public void Stop()
        {
            AppUpdater.Instance.KeepRunning = false;
        }

        private static TinyIoCContainer CreateContainer(ISpeedDateStartable startable)
        {
            var ioc = new TinyIoCContainer();
            try
            {
                //Register possible plugin-dependencies
                ioc.Register(AppUpdater.Instance);
                ioc.Register<ILogger>((container, overloads, requestType) => LogManager.GetLogger(requestType.Name));

                switch (startable)
                {
                    case IServer _:
                        ioc.Register((container, overloads, requesttype) => (IServer)startable);
                        break;
                    case IClient _:
                        ioc.Register((container, overloads, requesttype) => (IClient)startable);
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
                        ioc.Register(pluginConfig, pluginConfigType.FullName);
                    }

                    foreach (var pluginType in assembly.DefinedTypes.Where(info =>
                        !info.IsAbstract && !info.IsInterface && typeof(IPlugin).IsAssignableFrom(info)))
                    {
                        var plugin = Activator.CreateInstance(pluginType);
                        ioc.Register(plugin as IPlugin, pluginType.FullName);
                        ioc.Register(pluginType, (a, b, c) => plugin);
                    }
                    
                    foreach (var pluginResourceType in assembly.DefinedTypes.Where(info =>
                        !info.IsAbstract && !info.IsInterface))
                    {
                        if (IsAssignableToGenericType(pluginResourceType, typeof(IPluginResource<>), out var genericTypeArgument))
                        {
                            var pluginResource = Activator.CreateInstance(pluginResourceType);
                            ioc.Register(genericTypeArgument, pluginResource);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return ioc;
        }

        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }
        
        private static bool IsAssignableToGenericType(Type givenType, Type genericType, out Type genericTypeArgument)
        {
            genericTypeArgument = null;
            
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                {
                    genericTypeArgument = it.GenericTypeArguments.First();
                    return true;
                }
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                genericTypeArgument = givenType.GenericTypeArguments.First();
                return true;
            }

            var baseType = givenType.BaseType;
            return baseType != null && IsAssignableToGenericType(baseType, genericType, out genericTypeArgument);
        }

        public T GetPlugin<T>() where T : class, IPlugin
        {
            return _container.ResolveAll<IPlugin>().FirstOrDefault(plugin => plugin is T) as T;
        }
    }
}
