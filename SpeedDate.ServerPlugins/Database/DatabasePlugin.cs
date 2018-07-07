using System;
using Npgsql;
using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Database.CockroachDb;

namespace SpeedDate.ServerPlugins.Database
{
    internal class CockroachDbPlugin : IPlugin
    {
        public IDbAccess DbAccess;

        [Inject] private CockroachDbConfig _config;

        public void Loaded(IPluginProvider pluginProvider)
        {
            try
            {
                var connectionString = CommandLineArgs.IsProvided(CommandLineArgs.Names.DbConnectionString) ? 
                    CommandLineArgs.DbConnectionString : 
                    new NpgsqlConnectionStringBuilder
                    {
                        Host = _config.Host,
                        Port = _config.Port,
                        Username = _config.Username,
                        Password = _config.Password,
                        Database = _config.Database
                    }.ConnectionString;

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

                DbAccess = new CockroachDbAccess(connectionString);
            }
            catch
            {
                Logs.Error("Failed to connect to database");
                throw;
            }
        }
    }
}

