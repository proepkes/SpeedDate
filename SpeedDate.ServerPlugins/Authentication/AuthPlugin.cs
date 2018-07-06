using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Authentication;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Database.CockroachDb;
using SpeedDate.ServerPlugins.Mail;

namespace SpeedDate.ServerPlugins.Authentication
{
    /// <summary>
    ///     Authentication module, which handles logging in and registration of accounts
    /// </summary>
    public class AuthPlugin : SpeedDateServerPlugin
    {
        public delegate void AuthEventHandler(IUserExtension account);
        
        [Inject]
        private readonly ILogger _logger;

        [Inject]
        private readonly AuthConfig _config;

        private readonly Dictionary<string, IUserExtension> _loggedInUsers = new Dictionary<string, IUserExtension>();

        private CockroachDbPlugin _database;

        private MailPlugin _mailer;

        private int _nextGuestId;

        private readonly List<PermissionEntry> _permissions = new List<PermissionEntry>();

        /// <summary>
        ///     Invoked, when user logs in
        /// </summary>
        public event AuthEventHandler LoggedIn;

        /// <summary>
        ///     Invoked, when user logs out
        /// </summary>
        public event AuthEventHandler LoggedOut;

        public override void Loaded(IPluginProvider pluginProvider)
        {
            _database = pluginProvider.Get<CockroachDbPlugin>();
            _mailer = pluginProvider.Get<MailPlugin>();

            // Set handlers
            Server.SetHandler((ushort)OpCodes.LogIn, HandleLogIn);
            Server.SetHandler((ushort)OpCodes.LogOut, HandleLogOut);
            Server.SetHandler((ushort)OpCodes.RegisterAccount, HandleRegister);
            Server.SetHandler((ushort)OpCodes.PasswordResetCodeRequest, HandlePasswordResetRequest);
            Server.SetHandler((ushort)OpCodes.RequestEmailConfirmCode, HandleRequestEmailConfirmCode);
            Server.SetHandler((ushort)OpCodes.ConfirmEmail, HandleEmailConfirmation);
            Server.SetHandler((ushort)OpCodes.GetLoggedInCount, HandleGetLoggedInCount);
            Server.SetHandler((ushort)OpCodes.PasswordChange, HandlePasswordChange);
            Server.SetHandler((ushort)OpCodes.GetPeerAccountInfo, HandleGetPeerAccountInfo);

            // AesKey handler
            Server.SetHandler((ushort)OpCodes.AesKeyRequest, HandleAesKeyRequest);
            Server.SetHandler((ushort)OpCodes.RequestPermissionLevel, HandlePermissionLevelRequest);
        }

        private void HandleLogOut(IIncommingMessage message)
        {
            var extension = message.Peer.GetExtension<IUserExtension>();
            if (extension == null)
            {
                message.Respond("Not logged in", ResponseStatus.Failed);
                return;
            }

            message.Respond(ResponseStatus.Success);
        }

        public string GenerateGuestUsername()
        {
            return _config.GuestPrefix + _nextGuestId++;
        }

        public virtual IUserExtension CreateUserExtension(IPeer peer)
        {
            return new UserExtension(peer);
        }

        protected void FinalizeLogin(IUserExtension extension)
        {
            extension.Peer.Disconnected += OnUserDisconnect;

            // Add to lookup of logged in users
            _loggedInUsers.Add(extension.Username.ToLower(), extension);

            // Trigger the login event
            LoggedIn?.Invoke(extension);
        }

        private void OnUserDisconnect(IPeer peer)
        {
            var extension = peer.GetExtension<IUserExtension>();

            if (extension == null)
                return;

            _loggedInUsers.Remove(extension.Username.ToLower());

            peer.Disconnected -= OnUserDisconnect;

            LoggedOut?.Invoke(extension);
        }

        public IUserExtension GetLoggedInUser(string username)
        {
            _loggedInUsers.TryGetValue(username.ToLower(), out var extension);
            return extension;
        }

        protected virtual bool IsUsernameValid(string username)
        {
            return !string.IsNullOrEmpty(username) && // If username is empty
                   username == username.Replace(" ", ""); // If username contains spaces
        }

        protected virtual bool ValidateEmail(string email)
        {
            return !string.IsNullOrEmpty(email)
                   && email.Contains("@")
                   && email.Contains(".");
        }

        private bool HasGetPeerInfoPermissions(IPeer peer)
        {
            var extension = peer.GetExtension<PeerSecurityExtension>();
            return extension.PermissionLevel >= _config.PeerDataPermissionsLevel;
        }

        public bool IsUserLoggedIn(string username)
        {
            return _loggedInUsers.ContainsKey(username);
        }

        #region Message Handlers

        /// <summary>
        ///     Handles client's request to change password
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandlePasswordChange(IIncommingMessage message)
        {
            var data = new Dictionary<string, string>().FromBytes(message.AsBytes());

            if (!data.ContainsKey("code") || !data.ContainsKey("password") || !data.ContainsKey("email"))
            {
                message.Respond("Invalid request", ResponseStatus.Unauthorized);
                return;
            }

            var db = _database.AuthDatabase;

            var resetData = db.GetPasswordResetData(data["email"]);

            if (resetData?.Code == null || resetData.Code != data["code"])
            {
                message.Respond("Invalid code provided", ResponseStatus.Unauthorized);
                return;
            }

            var account = db.GetAccountByEmail(data["email"]);

            // Delete (overwrite) code used
            db.SavePasswordResetCode(account, null);

            account.Password = Util.CreateHash(data["password"]);
            db.UpdateAccount(account);

            message.Respond(ResponseStatus.Success);
        }

        /// <summary>
        ///     Handles a request to retrieve a number of logged in users
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleGetLoggedInCount(IIncommingMessage message)
        {
            message.Respond(_loggedInUsers.Count, ResponseStatus.Success);
        }

        /// <summary>
        ///     Handles e-mail confirmation request
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleEmailConfirmation(IIncommingMessage message)
        {
            var code = message.AsString();

            var extension = message.Peer.GetExtension<IUserExtension>();

            if (extension?.AccountData == null)
            {
                message.Respond("Invalid session", ResponseStatus.Unauthorized);
                return;
            }

            if (extension.AccountData.IsGuest)
            {
                message.Respond("Guests cannot confirm e-mails", ResponseStatus.Unauthorized);
                return;
            }

            if (extension.AccountData.IsEmailConfirmed)
            {
                // We still need to respond with "success" in case
                // response is handled somehow on the client
                message.Respond("Your email is already confirmed",
                    ResponseStatus.Success);
                return;
            }

            var db = _database.AuthDatabase;

            var requiredCode = db.GetEmailConfirmationCode(extension.AccountData.Email);

            if (requiredCode != code)
            {
                message.Respond("Invalid activation code", ResponseStatus.Error);
                return;
            }

            // Confirm e-mail
            extension.AccountData.IsEmailConfirmed = true;

            // Update account
            db.UpdateAccount(extension.AccountData);

            // Respond with success
            message.Respond(ResponseStatus.Success);
        }

        /// <summary>
        ///     Handles password reset request
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandlePasswordResetRequest(IIncommingMessage message)
        {
            var email = message.AsString();
            var db = _database.AuthDatabase;

            var account = db.GetAccountByEmail(email);

            if (account == null)
            {
                message.Respond("No such e-mail in the system", ResponseStatus.Unauthorized);
                return;
            }

            var code = Util.CreateRandomString(4);

            db.SavePasswordResetCode(account, code);

            if (!_mailer.SendMail(account.Email, "Password Reset Code", string.Format(_config.PasswordResetEmailBody, code)))
            {
                message.Respond("Couldn't send an activation code to your e-mail");
                return;
            }

            message.Respond(ResponseStatus.Success);
        }

        protected virtual void HandleRequestEmailConfirmCode(IIncommingMessage message)
        {
            var extension = message.Peer.GetExtension<IUserExtension>();

            if (extension?.AccountData == null)
            {
                message.Respond("Invalid session", ResponseStatus.Unauthorized);
                return;
            }

            if (extension.AccountData.IsGuest)
            {
                message.Respond("Guests cannot confirm e-mails", ResponseStatus.Unauthorized);
                return;
            }

            var code = Util.CreateRandomString(6);

            var db = _database.AuthDatabase;

            // Save the new code
            Debug.WriteLine("SHOULD BE HERE");
            db.SaveEmailConfirmationCode(extension.AccountData.Email, code);

            if (!_mailer.SendMail(extension.AccountData.Email, "E-mail confirmation",
                string.Format(_config.ConfirmEmailBody, code)))
            {
                message.Respond("Couldn't send a confirmation code to your e-mail. Please contact support");
                return;
            }

            // Respond with success
            message.Respond(ResponseStatus.Success);
        }

        /// <summary>
        ///     Handles account registration request
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleRegister(IIncommingMessage message)
        {
            var encryptedData = message.AsBytes();

            var securityExt = message.Peer.GetExtension<PeerSecurityExtension>();
            var aesKey = securityExt.AesKey;

            if (aesKey == null)
            {
                // There's no aesKey that client and master agreed upon
                message.Respond("Insecure request".ToBytes(), ResponseStatus.Unauthorized);
                return;
            }

            var decrypted = Util.DecryptAES(encryptedData, aesKey);
            var data = new Dictionary<string, string>().FromBytes(decrypted);

            if (!data.ContainsKey("username") || !data.ContainsKey("password") || !data.ContainsKey("email"))
            {
                message.Respond("Invalid registration request".ToBytes(), ResponseStatus.Error);
                return;
            }

            var username = data["username"];
            var password = data["password"];
            var email = data["email"].ToLower();

            var usernameLower = username.ToLower();

            var extension = message.Peer.GetExtension<IUserExtension>();

            if (extension != null && !extension.AccountData.IsGuest)
            {
                // Fail, if user is already logged in, and not with a guest account
                message.Respond("Invalid registration request".ToBytes(), ResponseStatus.Error);
                return;
            }

            if (!IsUsernameValid(usernameLower))
            {
                message.Respond("Invalid Username".ToBytes(), ResponseStatus.Error);
                return;
            }

            //TODO: WordFilter-Module
            //if (Config.ForbiddenUsernames.Contains(usernameLower))
            //{
            //    // Check if uses forbidden username
            //    message.Respond("Forbidden word used in username".ToBytes(), ResponseStatus.Error);
            //    return;
            //}

            //if (Config.ForbiddenWordsInUsernames.FirstOrDefault(usernameLower.Contains) != null)
            //{
            //    // Check if there's a forbidden word in username
            //    message.Respond("Forbidden word used in username".ToBytes(), ResponseStatus.Error);
            //    return;
            //}

            if (username.Length < _config.UsernameMinChars ||
                username.Length > _config.UsernameMaxChars)
            {
                // Check if username length is good
                message.Respond("Invalid usernanme length".ToBytes(), ResponseStatus.Error);

                return;
            }

            if (!ValidateEmail(email))
            {
                // Check if email is valid
                message.Respond("Invalid Email".ToBytes(), ResponseStatus.Error);
                return;
            }

            var db = _database.AuthDatabase;

            var account = db.CreateAccountObject();

            account.Username = username;
            account.Email = email;
            account.Password = Util.CreateHash(password);

            try
            {
                db.InsertNewAccount(account);

                message.Respond(ResponseStatus.Success);
            }
            catch (Exception e)
            {
                Logs.Error(e);
                message.Respond("Username or E-mail is already registered".ToBytes(), ResponseStatus.Error);
            }
        }

        /// <summary>
        ///     Handles a request to retrieve account information
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleGetPeerAccountInfo(IIncommingMessage message)
        {
            if (!HasGetPeerInfoPermissions(message.Peer))
            {
                message.Respond("Unauthorized", ResponseStatus.Unauthorized);
                return;
            }

            var peerId = message.AsInt();

            var peer = Server.GetPeer(peerId);

            if (peer == null)
            {
                message.Respond("Peer with a given ID is not in the game", ResponseStatus.Error);
                return;
            }

            var account = peer.GetExtension<IUserExtension>();

            if (account == null)
            {
                message.Respond("Peer has not been authenticated", ResponseStatus.Failed);
                return;
            }

            var data = account.AccountData;

            var packet = new PeerAccountInfoPacket
            {
                PeerId = peerId,
                Properties = data.Properties,
                Username = account.Username
            };

            message.Respond(packet, ResponseStatus.Success);
        }

        /// <summary>
        ///     Handles a request to log in
        /// </summary>
        /// <param name="message"></param>
        protected virtual void HandleLogIn(IIncommingMessage message)
        {
            if (message.Peer.HasExtension<IUserExtension>())
            {
                message.Respond("Already logged in", ResponseStatus.Unauthorized);
                return;
            }

            var encryptedData = message.AsBytes();
            var securityExt = message.Peer.GetExtension<PeerSecurityExtension>();
            var aesKey = securityExt.AesKey;

            if (aesKey == null)
            {
                // There's no aesKey that client and master agreed upon
                message.Respond("Insecure request".ToBytes(), ResponseStatus.Unauthorized);
                return;
            }

            var decrypted = Util.DecryptAES(encryptedData, aesKey);
            var data = new Dictionary<string, string>().FromBytes(decrypted);

            var db = _database.AuthDatabase;

            IAccountData accountData = null;

            // ---------------------------------------------
            // Guest Authentication
            if (data.ContainsKey("guest") && _config.EnableGuestLogin)
            {
                var guestUsername = GenerateGuestUsername();
                accountData = db.CreateAccountObject();

                accountData.Username = guestUsername;
                accountData.IsGuest = true;
                accountData.IsAdmin = false;
            }

            // ----------------------------------------------
            // Token Authentication
            if (data.ContainsKey("token") && accountData == null)
            {
                accountData = db.GetAccountByToken(data["token"]);
                if (accountData == null)
                {
                    message.Respond("Invalid Credentials".ToBytes(), ResponseStatus.Unauthorized);
                    return;
                }

                var otherSession = GetLoggedInUser(accountData.Username);
                if (otherSession != null)
                {
                    otherSession.Peer.Disconnect("Other user logged in");
                    message.Respond("This account is already logged in".ToBytes(),
                        ResponseStatus.Unauthorized);
                    return;
                }
            }

            // ----------------------------------------------
            // Username / Password authentication

            if (data.ContainsKey("username") && data.ContainsKey("password") && accountData == null)
            {
                var username = data["username"];
                var password = data["password"];

                accountData = db.GetAccount(username);

                if (accountData == null)
                {
                    // Couldn't find an account with this name
                    message.Respond("Invalid Credentials".ToBytes(), ResponseStatus.Unauthorized);
                    return;
                }

                if (!Util.ValidatePassword(password, accountData.Password))
                {
                    // Password is not correct
                    message.Respond("Invalid Credentials".ToBytes(), ResponseStatus.Unauthorized);
                    return;
                }
                
                var otherSession = GetLoggedInUser(accountData.Username);
                if (otherSession != null)
                {
                    otherSession.Peer.Disconnect("Other user logged in");
                    message.Respond("This account is already logged in".ToBytes(),
                        ResponseStatus.Unauthorized);
                    return;
                }
            }

            if (accountData == null)
            {
                message.Respond("Invalid request", ResponseStatus.Unauthorized);
                return;
            }

            // Setup auth extension
            var extension = message.Peer.AddExtension(CreateUserExtension(message.Peer));
            extension.Load(accountData);
            var infoPacket = extension.CreateInfoPacket();

            // Finalize login
            FinalizeLogin(extension);

            message.Respond(infoPacket.ToBytes(), ResponseStatus.Success);
        }


        protected virtual void HandlePermissionLevelRequest(IIncommingMessage message)
        {
            var key = message.AsString();

            var extension = message.Peer.GetExtension<PeerSecurityExtension>();

            var currentLevel = extension.PermissionLevel;
            var newLevel = currentLevel;

            var permissionClaimed = false;

            foreach (var entry in _permissions)
                if (entry.Key == key)
                {
                    newLevel = entry.PermissionLevel;
                    permissionClaimed = true;
                }

            extension.PermissionLevel = newLevel;

            if (!permissionClaimed && !string.IsNullOrEmpty(key))
            {
                // If we didn't claim a permission
                message.Respond("Invalid permission key", ResponseStatus.Unauthorized);
                return;
            }

            message.Respond(newLevel, ResponseStatus.Success);
        }

        protected virtual void HandleAesKeyRequest(IIncommingMessage message)
        {
            var extension = message.Peer.GetExtension<PeerSecurityExtension>();

            var encryptedKey = extension.AesKeyEncrypted;

            if (encryptedKey != null)
            {
                // There's already a key generated
                message.Respond(encryptedKey, ResponseStatus.Success);
                return;
            }

            // Generate a random key
            var aesKey = Util.CreateRandomString(8);

            var clientsPublicKeyXml = message.AsString();

            // Deserialize public key
            var sr = new StringReader(clientsPublicKeyXml);
            var xs = new XmlSerializer(typeof(RSAParameters));
            var clientsPublicKey = (RSAParameters) xs.Deserialize(sr);

            using (var csp = new RSACryptoServiceProvider())
            {
                csp.ImportParameters(clientsPublicKey);
                var encryptedAes = csp.Encrypt(Encoding.Unicode.GetBytes(aesKey), false);

                // Save keys for later use
                extension.AesKeyEncrypted = encryptedAes;
                extension.AesKey = aesKey;

                _logger.Debug("Sending " + encryptedAes + " to " + message.Peer.ConnectId);
                message.Respond(encryptedAes, ResponseStatus.Success);
            }
        }

        #endregion
    }
}
