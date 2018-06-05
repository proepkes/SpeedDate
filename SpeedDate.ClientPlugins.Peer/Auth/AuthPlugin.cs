using System;
using System.Collections.Generic;
using SpeedDate.ClientPlugins.Peer.Security;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Network;
using SpeedDate.Packets.Authentication;

namespace SpeedDate.ClientPlugins.Peer.Auth
{
    public class AuthPlugin : SpeedDateClientPlugin
    {
        private SecurityPlugin _securityPlugin;

        public delegate void LoginCallback(AccountInfoPacket accountInfo, string error);

        private bool _isLoggingIn;

        public bool IsLoggedIn { get; protected set; }

        public AccountInfoPacket AccountInfo;

        public event Action LoggedIn;
        public event Action Registered;
        public event Action LoggedOut;

        public AuthPlugin(IClientSocket clientSocket) : base(clientSocket)
        {
            _securityPlugin = new SecurityPlugin(clientSocket);
        }


        /// <summary>
        /// Sends a registration request to given connection
        /// </summary>
        public void Register(Dictionary<string, string> data, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected to server");
                return;
            }

            if (_isLoggingIn)
            {
                callback.Invoke(false, "Log in is already in progress");
                return;
            }

            if (IsLoggedIn)
            {
                callback.Invoke(false, "Already logged in");
                return;
            }

            // We first need to get an aes key 
            // so that we can encrypt our login data
            _securityPlugin.GetAesKey(aesKey =>
            {
                if (aesKey == null)
                {
                    callback.Invoke(false, "Failed to register due to security issues");
                    return;
                }

                var encryptedData = Util.EncryptAES(data.ToBytes(), aesKey);

                Connection.SendMessage((short)OpCodes.RegisterAccount, encryptedData, (status, response) =>
                {

                    if (status != ResponseStatus.Success)
                    {
                        callback.Invoke(false, response.AsString("Unknown error"));
                        return;
                    }

                    callback.Invoke(true, null);

                    Registered?.Invoke();
                });
            });
        }

        /// <summary>
        ///     Initiates a log out. In the process, disconnects and connects
        ///     back to the server to ensure no state data is left on the server.
        /// </summary>
        public void LogOut()
        {
            LogOut(Connection);
        }

        /// <summary>
        ///     Initiates a log out. In the process, disconnects and connects
        ///     back to the server to ensure no state data is left on the server.
        /// </summary>
        public void LogOut(IClientSocket connection)
        {
            if (!IsLoggedIn)
                return;

            IsLoggedIn = false;
            AccountInfo = null;

            if ((connection != null) && connection.IsConnected)
                connection.Reconnect();

            LoggedOut?.Invoke();
        }

        /// <summary>
        /// Sends a request to server, to log in as a guest
        /// </summary>
        public void LogInAsGuest(LoginCallback callback)
        {
            LogIn(new Dictionary<string, string>()
            {
                {"guest", "" }
            }, callback);
        }

        /// <summary>
        /// Sends a login request, using given credentials
        /// </summary>
        public void LogIn(string username, string password, LoginCallback callback)
        {
            LogIn(new Dictionary<string, string>
            {
                {"username", username},
                {"password", password}
            }, callback);
        }

        /// <summary>
        /// Sends a generic login request
        /// </summary>
        public void LogIn(Dictionary<string, string> data, LoginCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(null, "Not connected to server");
                return;
            }

            _isLoggingIn = true;

            // We first need to get an aes key 
            // so that we can encrypt our login data
            _securityPlugin.GetAesKey(aesKey =>
            {
                if (aesKey == null)
                {
                    _isLoggingIn = false;
                    callback.Invoke(null, "Failed to log in due to security issues");
                    return;
                }

                var encryptedData = Util.EncryptAES(data.ToBytes(), aesKey);

                Connection.SendMessage((short) OpCodes.LogIn, encryptedData, (status, response) =>
                {
                    _isLoggingIn = false;

                    if (status != ResponseStatus.Success)
                    {
                        callback.Invoke(null, response.AsString("Unknown error"));
                        return;
                    }

                    IsLoggedIn = true;

                    AccountInfo = response.Deserialize(new AccountInfoPacket());

                    callback.Invoke(AccountInfo, null);

                    LoggedIn?.Invoke();
                });
            });
        }


        /// <summary>
        /// Sends an e-mail confirmation code to the server
        /// </summary>
        public void ConfirmEmail(string code, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected to server");
                return;
            }

            if (!IsLoggedIn)
            {
                callback.Invoke(false, "You're not logged in");
                return;
            }

            Connection.SendMessage((short)OpCodes.ConfirmEmail, code, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }
        
        /// <summary>
        /// Sends a request to server, to ask for an e-mail confirmation code
        /// </summary>
        public void RequestEmailConfirmationCode(SuccessCallback callback, IClientSocket Connection)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected to server");
                return;
            }

            if (!IsLoggedIn)
            {
                callback.Invoke(false, "You're not logged in");
                return;
            }

            Connection.SendMessage((short)OpCodes.RequestEmailConfirmCode, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Sends a request to server, to ask for a password reset
        /// </summary>
        public void RequestPasswordReset(string email, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected to server");
                return;
            }

            Connection.SendMessage((short)OpCodes.PasswordResetCodeRequest, email, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }


        /// <summary>
        /// Sends a new password to server
        /// </summary>
        public void ChangePassword(PasswordChangeData data, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected to server");
                return;
            }

            var dictionary = new Dictionary<string, string>()
            {
                {"email", data.Email },
                {"code", data.Code },
                {"password", data.NewPassword }
            };

            Connection.SendMessage((short)OpCodes.PasswordChange, dictionary.ToBytes(), (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }
    }
}