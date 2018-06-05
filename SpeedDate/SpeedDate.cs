using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ninject;
using Ninject.Extensions.Conventions;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Networking;
using SpeedDate.Plugin;

namespace SpeedDate
{
    public sealed class SpeedDate
    {
        private readonly List<ISpeedDateListener> _listeners;

        public SpeedDate()
        {
            _listeners = new List<ISpeedDateListener>();
        }

        public void Start(string configFile)
        {
            SpeedDateConfig.Initialize(configFile);
            var logger = LogManager.GetLogger("SpeedDate");
            var kernel = CreateKernel();

            var pluginProver = kernel.Get<IPluginProvider>();
            foreach (var plugin in kernel.GetAll<IPlugin>()) pluginProver.RegisterPlugin(plugin);

            foreach (var plugin in pluginProver.GetAll())
            {
                plugin.Loaded(pluginProver);
                logger.Info($"Loaded {plugin.GetType().Name}");
            }

            var server = kernel.TryGet<IServer>();
            if (server != null)
                logger.Info("Found servertype: " + server.GetType().Name);

            var client = kernel.TryGet<IClient>();
            if (client != null)
                logger.Info("Found clienttype: " + client.GetType().Name);

            _listeners.Clear();
            foreach (var listener in kernel.GetAll<ISpeedDateListener>())
            {
                _listeners.Add(listener);

                listener.OnSpeedDateStarted();
            }
        }

        public void Stop()
        {
            foreach (var listener in _listeners)
            {
                listener.OnSpeedDateStopped();
                ;
            }
        }

        private static IKernel CreateKernel()
        {
            if (SpeedDateConfig.Plugins.CreateDirIfNotExists && !Directory.Exists(SpeedDateConfig.Plugins.SearchPath))
                Directory.CreateDirectory(SpeedDateConfig.Plugins.SearchPath);

            var kernel = new StandardKernel();

            try
            {
                kernel.Load("*Server.dll"); //Loads all Ninject-Modules in all *Server.dll-files. Example: Binds IServer to SpeedDateServer
                kernel.Load("*Client.dll"); 
                kernel.Bind<IClientSocket>().To<ClientSocket>();
                kernel.Bind<IServerSocket>().To<ServerSocket>();
                kernel.Bind<ILogger>().ToMethod(context =>
                    LogManager.GetLogger(context.Request.Target?.Member.DeclaringType?.Name));
                kernel.Bind<IPluginProvider>().To<NinjectPluginProvier>();
                kernel.Bind(x =>
                {
                    x.FromAssembliesMatching("*.dll")
                        .IncludingNonPublicTypes()
                        .SelectAllClasses()
                        .InheritedFrom<IPlugin>()
                        .BindDefaultInterfaces()
                        .Configure(syntax => syntax.InSingletonScope());
                });
            }
            catch (Exception ex)
            {
                if (ex is ReflectionTypeLoadException typeLoadException)
                {
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                    foreach (var loaderException in loaderExceptions) Console.WriteLine(loaderException);
                }

                throw;
            }

            return kernel;
        }
    }
}