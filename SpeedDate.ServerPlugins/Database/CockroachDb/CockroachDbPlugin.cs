using System;
using Npgsql;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;
using SpeedDate.ServerPlugins.Profiles;

namespace SpeedDate.ServerPlugins.Database.CockroachDb
{
    internal class CockroachDbPlugin : SpeedDateServerPlugin
    {
        public IAuthDatabase AuthDatabase;

        public IProfilesDatabase ProfilesDatabase;

        [Inject] private CockroachDbConfig _config;

        public override void Loaded(IPluginProvider pluginProvider)
        {
            base.Loaded(pluginProvider);
            try
            {
                var connStringBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = _config.Host,
                    Port = _config.Port,
                    Username = _config.Username,
                    Password = _config.Password,
                    Database = _config.Database
                };

                var connectionString = CommandLineArgs.IsProvided(CommandLineArgs.Names.DbConnectionString)
                    ? CommandLineArgs.DbConnectionString
                    : connStringBuilder.ConnectionString;

                try
                {
                    if (_config.CheckConnectionOnStartup)
                    {
                        using (var con = new NpgsqlConnection(connectionString))
                        {
                            con.Open();
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
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

