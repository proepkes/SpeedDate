using SpeedDate.Packets;
using SpeedDate.ServerPlugins.Authentication;

namespace SpeedDate.ServerPlugins.Database
{
    public interface IDbAccess
    {
        /// <summary>
        ///     Should create an empty object with account data.
        /// </summary>
        /// <returns></returns>
        IAccountData CreateAccountObject();

        IAccountData GetAccount(string username);
        IAccountData GetAccountByToken(string token);
        IAccountData GetAccountByEmail(string email);

        void SavePasswordResetCode(IAccountData account, string code);
        IPasswordResetData GetPasswordResetData(string email);

        void SaveEmailConfirmationCode(string email, string code);
        string GetEmailConfirmationCode(string email);

        void UpdateAccount(IAccountData account);
        void InsertNewAccount(IAccountData account);
        void InsertToken(IAccountData account, string token);
        
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
