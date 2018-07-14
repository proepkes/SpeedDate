using System;
using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Packets;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.ServerPlugins.Database.Entities;

namespace SpeedDate.ServerPlugins.Database
{
    ///<summary>   
    /// A database plugin which wraps a concrete dbAccess. 
    /// This class cannot be inherited. 
    ///</summary>
    public sealed class DatabasePlugin : IPlugin, IDbAccess
    {
        [Inject] private ILogger _logger;
        [Inject] private DatabaseConfig _config;
        [Inject] private IDbAccess _concreteDbAccess;

        public void Loaded()
        {
            try
            {
                var connectionString = CommandLineArgs.IsProvided(CommandLineArgs.DbConnectionString)
                    ? CommandLineArgs.DbConnectionString
                    : _concreteDbAccess.BuildConnectionString(_config);
                   

                _concreteDbAccess.SetConnectionString(connectionString);
                
                if (_config.CheckConnectionOnStartup && !_concreteDbAccess.TryConnection(out var e))
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
            _concreteDbAccess = dbAccess;

            if (_config.CheckConnectionOnStartup && !_concreteDbAccess.TryConnection(out var e))
            {
                _logger.Error(e);
                throw e;
            }
        }

        public string BuildConnectionString(DatabaseConfig config)
        {
            return _concreteDbAccess.BuildConnectionString(config);
        }

        public void SetConnectionString(string connectionString)
        {
            _concreteDbAccess.SetConnectionString(connectionString);
        }

        public bool TryConnection(out Exception e)
        {
            return _concreteDbAccess.TryConnection(out e);
        }

        public AccountData CreateAccountObject()
        {
            return _concreteDbAccess.CreateAccountObject();
        }

        public AccountData GetAccount(string username)
        {
            return _concreteDbAccess.GetAccount(username);
        }

        public AccountData GetAccountByToken(string token)
        {
            return _concreteDbAccess.GetAccountByToken(token);
        }

        public AccountData GetAccountByEmail(string email)
        {
            return _concreteDbAccess.GetAccountByEmail(email);
        }

        public void SavePasswordResetCode(AccountData account, string code)
        {
            _concreteDbAccess.SavePasswordResetCode(account, code);
        }

        public PasswordResetData GetPasswordResetData(string email)
        {
            return _concreteDbAccess.GetPasswordResetData(email);
        }

        public void SaveEmailConfirmationCode(string email, string code)
        {
            _concreteDbAccess.SaveEmailConfirmationCode(email, code);
        }

        public string GetEmailConfirmationCode(string email)
        {
            return _concreteDbAccess.GetEmailConfirmationCode(email);
        }

        public void UpdateAccount(AccountData account)
        {
            _concreteDbAccess.UpdateAccount(account);
        }

        public void InsertNewAccount(AccountData account)
        {
            _concreteDbAccess.InsertNewAccount(account);
        }

        public void InsertToken(AccountData account, string token)
        {
            _concreteDbAccess.InsertToken(account, token);
        }

        public void RestoreProfile(ObservableServerProfile profile)
        {
            _concreteDbAccess.RestoreProfile(profile);
        }

        public void UpdateProfile(ObservableServerProfile profile)
        {
            _concreteDbAccess.UpdateProfile(profile);
        }
    }
}

