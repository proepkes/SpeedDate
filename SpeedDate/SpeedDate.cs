using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Interfaces.Plugins;
using SpeedDate.Logging;
using SpeedDate.Network;
using TinyIoC;

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

            var startable = kernel.Resolve<ISpeedDateStartable>();
            startable.Started += () => Started?.Invoke();
            startable.Stopped += () => Stopped?.Invoke();

            PluginProver = kernel.Resolve<IPluginProvider>();

            foreach (var plugin in kernel.ResolveAll<IPlugin>())
                PluginProver.RegisterPlugin(plugin);

            foreach (var plugin in PluginProver.GetAll())
            {
                plugin.Loaded(PluginProver);
                logger.Info($"Loaded {plugin.GetType().Name}");
            }

            if(kernel.TryResolve(out IServer server))
                logger.Info("Acting as server: " + server.GetType().Name);

            if(kernel.TryResolve(out IClient client))
                logger.Info("Acting as client: " + client.GetType().Name);

            startable.Start();
        }

        public void Stop()
        {
            Stopped?.Invoke();
        }

        private static TinyIoCContainer CreateKernel()
        {
            try
            {
                foreach (var dllFile in
                    Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "*.dll"))
                {
                    var assembly = Assembly.LoadFrom(dllFile);
                    foreach (var pluginAssembly in assembly.DefinedTypes.Where(info =>
                        !info.IsAbstract && !info.IsInterface && typeof(ISpeedDateModule).IsAssignableFrom(info)))
                    {
                        ((ISpeedDateModule)Activator.CreateInstance(pluginAssembly)).Load(TinyIoCContainer.Current);
                    }
                }

                TinyIoCContainer.Current.Register<IClientSocket, ClientSocket>();
                TinyIoCContainer.Current.Register<IServerSocket, ServerSocket>();
                TinyIoCContainer.Current.Register<IPluginProvider, PluginProvider>();
                TinyIoCContainer.Current.Register<ILogger>((container, overloads) => LogManager.GetLogger("Test"));
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
