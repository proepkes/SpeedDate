using System;
using SpeedDate.Packets;
using SpeedDate.ServerPlugins.Database.Entities;

namespace SpeedDate.ServerPlugins.Database
{
    public interface IDbAccess
    {
        string BuildConnectionString(DatabaseConfig config);
        void SetConnectionString(string connectionString);

        bool TryConnection(out Exception e);
        
        AccountData CreateAccountObject();

        AccountData GetAccount(string username);
        AccountData GetAccountByToken(string token);
        AccountData GetAccountByEmail(string email);

        void SavePasswordResetCode(AccountData account, string code);
        PasswordResetData GetPasswordResetData(string email);

        void SaveEmailConfirmationCode(string email, string code);
        string GetEmailConfirmationCode(string email);

        void UpdateAccount(AccountData account);
        void InsertNewAccount(AccountData account);
        void InsertToken(AccountData account, string token);
        
        /// <summary>
        /// Should restore all values of the given profile, 
        /// or not change them, if there's no entry in the database
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        void RestoreProfile(ObservableServerProfile profile);

        /// <summary>
        /// Should save updated profile into database
        /// </summary>
        /// <param name="profile"></param>
        void UpdateProfile(ObservableServerProfile profile);
    }
}
