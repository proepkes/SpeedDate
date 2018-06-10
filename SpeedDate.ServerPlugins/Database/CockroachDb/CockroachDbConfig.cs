using SpeedDate.Configuration;

namespace SpeedDate.ServerPlugins.Database.CockroachDb
{
    [PluginConfiguration(typeof(CockroachDbPlugin))]
    public class CockroachDbConfig
    {
        public bool CheckConnectionOnStartup { get; set; }

        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
    }
}

