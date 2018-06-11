
using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets;

namespace SpeedDate.ClientPlugins.Peer.Profiles
{
    public class ProfilePlugin : SpeedDateClientPlugin
    {
        /// <summary>
        ///     Sends a request to server, retrieves all profile values, and applies them to a provided
        ///     profile
        /// </summary>
        public void GetProfileValues(ObservableProfile profile, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            Connection.SendMessage((short) OpCodes.ClientProfileRequest, profile.PropertyCount, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                // Use the bytes received, to replicate the profile
                profile.FromBytes(response.AsBytes());

                // Listen to profile updates, and apply them
                Connection.SetHandler((short) OpCodes.UpdateClientProfile,
                    message => { profile.ApplyUpdates(message.AsBytes()); });

                callback.Invoke(true, null);
            });
        }
    }
}