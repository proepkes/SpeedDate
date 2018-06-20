using System.Collections.Generic;

using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Rooms;

namespace SpeedDate.ClientPlugins.Peer.Rooms
{
    public delegate void RoomAccessCallback(RoomAccessPacket access);

    public delegate void RoomAccessReceivedHandler(RoomAccessPacket access);

    public class RoomPlugin : SpeedDateClientPlugin
    {
        /// <summary>
        ///     An access, which was last received
        /// </summary>
        public RoomAccessPacket LastReceivedAccess { get; private set; }

        /// <summary>
        ///     Event, invoked when an access is received
        /// </summary>
        public event RoomAccessReceivedHandler AccessReceived;

        /// <summary>
        ///     Tries to get an access to a room with a given room id, password,
        ///     and some other properties, which will be visible to the room (game server)
        /// </summary>
        public void GetAccess(int roomId, RoomAccessCallback callback, string password,
            Dictionary<string, string> properties, ErrorCallback errorCallback)
        {
            if (!Connection.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            var packet = new RoomAccessRequestPacket
            {
                RoomId = roomId,
                Properties = properties,
                Password = password
            };

            Connection.SendMessage((ushort) OpCodes.GetRoomAccess, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown Error"));
                    return;
                }

                var access = response.Deserialize(new RoomAccessPacket());

                LastReceivedAccess = access;

                callback.Invoke(access);

                AccessReceived?.Invoke(access);

                if (RoomConnector.Instance != null)
                    RoomConnector.Connect(access);
            });
        }

        /// <summary>
        ///     This method triggers the <see cref="AccessReceived" /> event. Call this,
        ///     if you made some custom functionality to get access to rooms
        /// </summary>
        /// <param name="access"></param>
        public void TriggerAccessReceivedEvent(RoomAccessPacket access)
        {
            LastReceivedAccess = access;

            AccessReceived?.Invoke(access);
        }
    }
}
