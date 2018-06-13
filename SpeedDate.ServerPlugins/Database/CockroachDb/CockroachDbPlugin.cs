using Npgsql;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Profiles;

namespace SpeedDate.ServerPlugins.Database.CockroachDb
{
    internal class CockroachDbPlugin : SpeedDateServerPlugin
    {
        public readonly IAuthDatabase AuthDatabase;

        public readonly IProfilesDatabase ProfilesDatabase;

        public CockroachDbPlugin()
        {
            var config = SpeedDateConfig.Get<CockroachDbConfig>();

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

