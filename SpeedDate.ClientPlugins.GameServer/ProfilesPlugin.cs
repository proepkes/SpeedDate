using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Utils.Conversion;
using SpeedDate.Network.Utils.IO;
using SpeedDate.Packets;

namespace SpeedDate.ClientPlugins.GameServer
{
    public class ProfilesPlugin : SpeedDateClientPlugin
    {
        /// <summary>
        /// Time, after which game server will try sending profile 
        /// updates to master server
        /// </summary>
        public float ProfileUpdatesInterval = 0.1f;

        private readonly Dictionary<string, ObservableServerProfile> _profiles;

        private readonly HashSet<ObservableServerProfile> _modifiedProfiles;

        private Task _sendUpdatesCoroutine;

        public ProfilesPlugin()
        {
            _profiles = new Dictionary<string, ObservableServerProfile>();
            _modifiedProfiles = new HashSet<ObservableServerProfile>();
        }

        /// <summary>
        /// Sends a request to server, retrieves all profile values, and applies them to a provided
        /// profile
        /// </summary>
        public void FillProfileValues(ObservableServerProfile profile, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            Connection.SendMessage((ushort) OpCodes.ServerProfileRequest, profile.Username, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                // Use the bytes received, to replicate the profile
                profile.FromBytes(response.AsBytes());

                profile.ClearUpdates();

                _profiles[profile.Username] = profile;

                profile.ModifiedInServer += serverProfile =>
                {
                    OnProfileModified(profile);
                };

                profile.Disposed += OnProfileDisposed;

                callback.Invoke(true, null);
            });
        }

        private void OnProfileModified(ObservableServerProfile profile)
        {
            _modifiedProfiles.Add(profile);

            if (_sendUpdatesCoroutine != null)
                return;

            _sendUpdatesCoroutine = Task.Factory.StartNew(() => KeepSendingUpdates(), TaskCreationOptions.LongRunning);
        }

        private void OnProfileDisposed(ObservableServerProfile profile)
        {
            profile.Disposed -= OnProfileDisposed;

            _profiles.Remove(profile.Username);
        }

        private async void KeepSendingUpdates()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(ProfileUpdatesInterval));

                if (_modifiedProfiles.Count == 0)
                    continue;

                using (var ms = new MemoryStream())
                using (var writer = new EndianBinaryWriter(EndianBitConverter.Big, ms))
                {
                    // Write profiles count
                    writer.Write(_modifiedProfiles.Count);

                    foreach (var profile in _modifiedProfiles)
                    {
                        // Write username
                        writer.Write(profile.Username);

                        var updates = profile.GetUpdates();

                        // Write updates length
                        writer.Write(updates.Length);

                        // Write updates
                        writer.Write(updates);

                        profile.ClearUpdates();
                    }

                    Connection.SendMessage((ushort) OpCodes.UpdateServerProfile, ms.ToArray());
                }

                _modifiedProfiles.Clear();
            }
        }
    }
}