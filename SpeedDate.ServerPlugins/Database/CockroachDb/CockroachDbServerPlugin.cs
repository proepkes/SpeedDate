using Npgsql;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Profiles;

namespace SpeedDate.ServerPlugins.Database.CockroachDb
{
    class CockroachDbServerPlugin : ServerPluginBase
    {
        public readonly IAuthDatabase AuthDatabase;

        public readonly IProfilesDatabase ProfilesDatabase;

        public CockroachDbServerPlugin(IServer server) : base(server)
        {
            var config = SpeedDateConfig.GetPluginConfig<CockroachDbConfig>();

            try
            {
                var connStringBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = config.Host,
                    Port = config.Port,
                    Username = config.Username,
                    Password = config.Password,
                    Database = config.Database
                };

                var connectionString = CommandLineArgs.IsProvided(CommandLineArgs.Names.DbConnectionString)
                    ? CommandLineArgs.DbConnectionString
                    : connStringBuilder.ConnectionString;

                if (config.CheckConnectionOnStartup)
                {
                    using (var con = new NpgsqlConnection(connectionString))
                    {
                        con.Open();
                    }
                }

                AuthDatabase = new AuthDbCockroachDb(connectionString);
                ProfilesDatabase = new ProfilesDbCockroachDb(connectionString);
            }
            catch
            {
                Logs.Error("Failed to connect to database");
                throw;
            }
        }
    }
}

