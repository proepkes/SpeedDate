using System;
using System.Net;
using SpeedDate.Configuration;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;

namespace SpeedDate.Test
{
    public class ServerFixture: IDisposable
    {
        public const string GuestPrefix = "TestGuest-";
        
        public readonly int Port = 12345;
        public readonly SpeedDateServer Server;
        
        public ServerFixture()
        {
            Server = new SpeedDateServer();
            
            Server.Start(new DefaultConfigProvider(new NetworkConfig(IPAddress.Any, Port), PluginsConfig.DefaultServerPlugins, new []
            {
                new AuthConfig
                {
                    GuestPrefix = GuestPrefix,
                    EnableGuestLogin = true
                }
            }));
        }
        
        public void Dispose()
        {
            Server.Stop();
            Server.Dispose();
        }
    }
}
