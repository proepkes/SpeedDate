using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Ninject;
using Ninject.Extensions.Conventions;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Interfaces.Plugins;
using SpeedDate.Logging;
using SpeedDate.Network;

namespace SpeedDate
{
    public sealed class SpeedDate
    {
        private readonly string _configFile;

        public event Action Started;
        public event Action Stopped;

        public IPluginProvider PluginProver
        {
            get;
            private set;
        }

        public SpeedDate(string configFile)
        {
            _configFile = configFile;
        }

        public void Start()
        {
            SpeedDateConfig.Initialize(_configFile);
            var logger = LogManager.GetLogger("SpeedDate");
            var kernel = CreateKernel();

            var startable = kernel.Get<ISpeedDateStartable>();
            startable.Started += () => Started?.Invoke();
            startable.Stopped += () => Stopped?.Invoke();

            PluginProver = kernel.Get<IPluginProvider>();

            foreach (var plugin in kernel.GetAll<IPlugin>())
                PluginProver.RegisterPlugin(plugin);

            foreach (var plugin in PluginProver.GetAll())
            {
                plugin.Loaded(PluginProver);
                logger.Info($"Loaded {plugin.GetType().Name}");
            }

            var server = kernel.TryGet<IServer>();
            if (server != null)
                logger.Info("Acting as server: " + server.GetType().Name);

            var client = kernel.TryGet<IClient>();
            if (client != null)
                logger.Info("Acting as client: " + client.GetType().Name);

            startable.Start();
        }

        public void Stop()
        {
            Stopped?.Invoke();
        }

        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();

            try
            {
                kernel.Load("*Server.dll"); //Loads all Ninject-Modules in all *Server.dll-files. Example: Binds IServer to SpeedDateServer
                kernel.Load("*Client.dll"); 
                kernel.Bind<IClientSocket>().To<ClientSocket>().InSingletonScope();
                kernel.Bind<IServerSocket>().To<ServerSocket>().InSingletonScope();
                kernel.Bind<ILogger>().ToMethod(context => LogManager.GetLogger(context.Request.Target?.Member.DeclaringType?.Name));
                kernel.Bind<IPluginProvider>().To<NinjectPluginProvier>().InSingletonScope();
                kernel.Bind(x =>
                {
                    x.FromAssembliesMatching("*.dll")
                        .IncludingNonPublicTypes()
                        .SelectAllClasses()
                        .InheritedFrom<IPlugin>()
                        .Where(type => SpeedDateConfig.Plugins.LoadAll ||
                                       type.Namespace != null && 
                                       SpeedDateConfig.Plugins.PluginsNamespace.Split(';').FirstOrDefault(ns => Regex.IsMatch(type.Namespace, WildCardToRegular(ns))) != null)
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
        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }
    }
}