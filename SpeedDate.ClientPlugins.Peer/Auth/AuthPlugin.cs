using System;
using System.Collections.Generic;
using SpeedDate.ClientPlugins.Peer.Security;
using SpeedDate.Network;
using SpeedDate.Packets.Authentication;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.ClientPlugins.Peer.Auth
{
    public class AuthPlugin : SpeedDateClientPlugin
    {
        private SecurityPlugin _securityPlugin;

        public delegate void LoginCallback(AccountInfoPacket accountInfo);

        private bool _isLoggingIn;

        public bool IsLoggedIn { get; protected set; }

        public AccountInfoPacket AccountInfo;
        
        public override void Loaded(IPluginProvider pluginProvider)
        {
            base.Loaded(pluginProvider);
            _securityPlugin = pluginProvider.Get<SecurityPlugin>();
        }

        /// <summary>
        /// Sends a registration request to given connection
        /// </summary>
        public void Register(Dictionary<string, string> data, SuccessCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected to server");
                return;
            }

            if (_isLoggingIn)
            {
                errorCallback.Invoke("Log in is already in progress");
                return;
            }

            if (IsLoggedIn)
            {
                errorCallback.Invoke("Already logged in");
                return;
            }

            // We first need to get an aes key 
            // so that we can encrypt our login data
            _securityPlugin.GetAesKey(aesKey =>
            {
                if (aesKey == null)
                {
                    errorCallback.Invoke("Failed to register due to security issues");
                    return;
                }

                var encryptedData = Util.EncryptAES(data.ToBytes(), aesKey);

                Client.SendMessage((ushort)OpCodes.RegisterAccount, encryptedData, (status, response) =>
                {

                    if (status != ResponseStatus.Success)
                    {
                        errorCallback.Invoke(response.AsString("Unknown error"));
                        return;
                    }

                    callback.Invoke();
                });
            });
        }

        /// <summary>
        ///     Initiates a log out. In the process, disconnects and connects
        ///     back to the server to ensure no state data is left on the server.
        /// </summary>
        public void LogOut()
        {
            if (!IsLoggedIn)
                return;

            IsLoggedIn = false;
            AccountInfo = null;
            _securityPlugin.ClearData();

            if (Client != null && Client.IsConnected)
                Client.Reconnect();
        }

        /// <summary>
        /// Sends a request to server, to log in as a guest
        /// </summary>
        public void LogInAsGuest(LoginCallback callback, ErrorCallback errorCallback)
        {
            LogIn(new Dictionary<string, string>()
            {
                {"guest", "" }
            }, callback, errorCallback);
        }

        /// <summary>
        /// Sends a login request, using given credentials
        /// </summary>
        public void LogIn(string username, string password, LoginCallback callback, ErrorCallback errorCallback)
        {
            LogIn(new Dictionary<string, string>
            {
                {"username", username},
                {"password", password}
            }, callback, errorCallback);
        }

        /// <summary>
        /// Sends a generic login request
        /// </summary>
        public void LogIn(Dictionary<string, string> data, LoginCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected to server");
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
                    errorCallback.Invoke("Failed to log in due to security issues");
                    return;
                }

                var encryptedData = Util.EncryptAES(data.ToBytes(), aesKey);

                Client.SendMessage((ushort) OpCodes.LogIn, encryptedData, (status, response) =>
                {
                    _isLoggingIn = false;
                    
                    if (status != ResponseStatus.Success)
                    {
                        errorCallback.Invoke(response.AsString("Unknown error"));
                        return;
                    }

                    IsLoggedIn = true;

                    AccountInfo = response.Deserialize<AccountInfoPacket>();

                    callback.Invoke(AccountInfo);
                });
            });
        }


        /// <summary>
        /// Sends an e-mail confirmation code to the server
        /// </summary>
        public void ConfirmEmail(string code, SuccessCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected to server");
                return;
            }

            if (!IsLoggedIn)
            {
                errorCallback.Invoke("You're not logged in");
                return;
            }

            Client.SendMessage((ushort)OpCodes.ConfirmEmail, code, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke();
            });
        }
        
        /// <summary>
        /// Sends a request to server, to ask for an e-mail confirmation code
        /// </summary>
        public void RequestEmailConfirmationCode(SuccessCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected to server");
                return;
            }

            if (!IsLoggedIn)
            {
                errorCallback.Invoke("You're not logged in");
                return;
            }

            Client.SendMessage((ushort)OpCodes.RequestEmailConfirmCode, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke();
            });
        }

        /// <summary>
        /// Sends a request to server, to ask for a password reset
        /// </summary>
        public void RequestPasswordReset(string email, SuccessCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected to server");
                return;
            }

            Client.SendMessage((ushort)OpCodes.PasswordResetCodeRequest, email, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke();
            });
        }


        /// <summary>
        /// Sends a new password to server
        /// </summary>
        public void ChangePassword(PasswordChangeData data, SuccessCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected to server");
                return;
            }

            var dictionary = new Dictionary<string, string>()
            {
                {"email", data.Email },
                {"code", data.Code },
                {"password", data.NewPassword }
            };

            Client.SendMessage((ushort)OpCodes.PasswordChange, dictionary.ToBytes(), (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke();
            });
        }
    }
}
