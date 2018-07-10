using System;
using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Packets;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.ServerPlugins.Database.Entities;

namespace SpeedDate.ServerPlugins.Database
{
    public class DatabasePlugin : IPlugin, IDbAccess
    {
        [Inject] private IDbAccess DbAccess;
        [Inject] private ILogger _logger;
        [Inject] private DatabaseConfig _config;
        
        public void Loaded()
        {
            try
            {
                var connectionString = CommandLineArgs.IsProvided(CommandLineArgs.Names.DbConnectionString)
                    ? CommandLineArgs.DbConnectionString
                    : DbAccess.BuildConnectionString(_config);
                   

                DbAccess.SetConnectionString(connectionString);
                
                if (_config.CheckConnectionOnStartup && !DbAccess.TryConnection(out var e))
                {
                    _logger.Error(e);
                    throw e;
                }
            }
            catch
            {
                Logs.Error("Failed to connect to database");
                throw;
            }
        }

        /// <summary>
        /// Sets the Database-Access. The previous one will be overwritten
        /// </summary>
        /// <param name="dbAccess"></param>
        public void SetDbAccess(IDbAccess dbAccess)
        {
            DbAccess = dbAccess;

            if (_config.CheckConnectionOnStartup && !DbAccess.TryConnection(out var e))
            {
                _logger.Error(e);
                throw e;
            }
        }

        public string BuildConnectionString(DatabaseConfig config)
        {
            return DbAccess.BuildConnectionString(config);
        }

        public void SetConnectionString(string connectionString)
        {
            DbAccess.SetConnectionString(connectionString);
        }

        public bool TryConnection(out Exception e)
        {
            return DbAccess.TryConnection(out e);
        }

        public AccountData CreateAccountObject()
        {
            return DbAccess.CreateAccountObject();
        }

        public AccountData GetAccount(string username)
        {
            return DbAccess.GetAccount(username);
        }

        public AccountData GetAccountByToken(string token)
        {
            return DbAccess.GetAccountByToken(token);
        }

        public AccountData GetAccountByEmail(string email)
        {
            return DbAccess.GetAccountByEmail(email);
        }

        public void SavePasswordResetCode(AccountData account, string code)
        {
            DbAccess.SavePasswordResetCode(account, code);
        }

        public PasswordResetData GetPasswordResetData(string email)
        {
            return DbAccess.GetPasswordResetData(email);
        }

        public void SaveEmailConfirmationCode(string email, string code)
        {
            DbAccess.SaveEmailConfirmationCode(email, code);
        }

        public string GetEmailConfirmationCode(string email)
        {
            return DbAccess.GetEmailConfirmationCode(email);
        }

        public void UpdateAccount(AccountData account)
        {
            DbAccess.UpdateAccount(account);
        }

        public void InsertNewAccount(AccountData account)
        {
            DbAccess.InsertNewAccount(account);
        }

        public void InsertToken(AccountData account, string token)
        {
            DbAccess.InsertToken(account, token);
        }

        public void RestoreProfile(ObservableServerProfile profile)
        {
            DbAccess.RestoreProfile(profile);
        }

        public void UpdateProfile(ObservableServerProfile profile)
        {
            DbAccess.UpdateProfile(profile);
        }
    }
}

