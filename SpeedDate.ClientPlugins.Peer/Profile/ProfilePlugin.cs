using SpeedDate.Network;
using SpeedDate.Packets;

namespace SpeedDate.ClientPlugins.Peer.Profile
{
    public class ProfilePlugin : SpeedDateClientPlugin
    {
        /// <summary>
        ///     Sends a request to server, retrieves all profile values, and applies them to a provided
        ///     profile
        /// </summary>
        public void GetProfileValues(ObservableProfile profile, SuccessCallback callback, ErrorCallback errorCallback)
        {
            if (!Connection.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            Connection.SendMessage((ushort) OpCodes.ClientProfileRequest, profile.PropertyCount, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                // Use the bytes received, to replicate the profile
                profile.FromBytes(response.AsBytes());

                // Listen to profile updates, and apply them
                Connection.SetHandler((ushort) OpCodes.UpdateClientProfile,
                    message => { profile.ApplyUpdates(message.AsBytes()); });

                callback.Invoke();
            });
        }
    }
}
