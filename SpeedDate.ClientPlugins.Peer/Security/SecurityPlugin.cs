using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;

namespace SpeedDate.ClientPlugins.Peer.Security
{
    /// <summary>
    ///     Helper class, which implements means to encrypt and decrypt data
    /// </summary>
    public class SecurityPlugin : SpeedDateClientPlugin
    {
        public delegate void PermissionLevelCallback(int? permissionLevel, string error);

        private const int RsaKeySize = 512;

        private readonly Dictionary<IClientSocket, EncryptionData> _encryptionData;

        public int CurrentPermissionLevel { get; private set; }

        public SecurityPlugin()
        {
            _encryptionData = new Dictionary<IClientSocket, EncryptionData>();
        }

        public event Action PermissionsLevelChanged;

        public void RequestPermissionLevel(string key, PermissionLevelCallback callback)
        {
            Connection.SendMessage((ushort) OpCodes.RequestPermissionLevel, key, (status, response) =>
            {
                if (status != ResponseStatus.Success) callback.Invoke(null, response.AsString("Unknown error"));

                CurrentPermissionLevel = response.AsInt();

                PermissionsLevelChanged?.Invoke();

                callback.Invoke(CurrentPermissionLevel, null);
            });
        }

        /// <summary>
        ///     Should be called on client. Generates RSA public key,
        ///     sends it to master, which returns encrypted AES key. After decrypting AES key,
        ///     callback is invoked with the value. You can then use the AES key to encrypt data
        /// </summary>
        public void GetAesKey(Action<string> callback)
        {
            _encryptionData.TryGetValue(Connection, out var data);

            if (data == null)
            {
                data = new EncryptionData();
                _encryptionData[Connection] = data;
                Connection.Disconnected += OnEncryptableConnectionDisconnected;

                data.ClientsCsp = new RSACryptoServiceProvider(RsaKeySize);

                // Generate keys
                data.ClientsPublicKey = data.ClientsCsp.ExportParameters(false);
            }

            if (data.ClientAesKey != null)
            {
                // We already have an aes generated for this connection
                callback.Invoke(data.ClientAesKey);
                return;
            }

            // Serialize public key
            var sw = new StringWriter();
            var xs = new XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, data.ClientsPublicKey);

            // Send the request
            Connection.SendMessage((ushort) OpCodes.AesKeyRequest, sw.ToString(), (status, response) =>
            {
                if (data.ClientAesKey != null)
                {
                    // Aes is already decrypted.
                    callback.Invoke(data.ClientAesKey);
                    return;
                }

                if (status != ResponseStatus.Success)
                {
                    // Failed to get an aes key
                    callback.Invoke(null);
                    return;
                }

                var decrypted = data.ClientsCsp.Decrypt(response.AsBytes(), false);
                data.ClientAesKey = Encoding.Unicode.GetString(decrypted);

                callback.Invoke(data.ClientAesKey);
            });
        }

        private void OnEncryptableConnectionDisconnected()
        {
            var disconnected = _encryptionData.Keys.Where(c => !c.IsConnected).ToList();

            foreach (var connection in disconnected)
            {
                // Remove encryption data
                _encryptionData.Remove(connection);

                // Unsubscribe from event
                connection.Disconnected -= OnEncryptableConnectionDisconnected;
            }
        }

        private class EncryptionData
        {
            public string ClientAesKey;
            public RSACryptoServiceProvider ClientsCsp;
            public RSAParameters ClientsPublicKey;
        }
    }
}