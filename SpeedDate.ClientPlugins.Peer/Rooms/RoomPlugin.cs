using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Network;
using SpeedDate.Packets.Rooms;

namespace SpeedDate.ClientPlugins.Peer.Rooms
{
    public delegate void RoomAccessCallback(RoomAccessPacket access, string error);

    public delegate void RoomAccessReceivedHandler(RoomAccessPacket access);

    public class RoomPlugin : SpeedDateClientPlugin
    {
        /// <summary>
        ///     An access, which was last received
        /// </summary>
        public RoomAccessPacket LastReceivedAccess { get; private set; }

        public RoomPlugin(IClientSocket clientSocket) : base(clientSocket)
        {
        }

        /// <summary>
        ///     Event, invoked when an access is received
        /// </summary>
        public event RoomAccessReceivedHandler AccessReceived;

        /// <summary>
        ///     Tries to get an access to a room with a given room id, password,
        ///     and some other properties, which will be visible to the room (game server)
        /// </summary>
        public void GetAccess(int roomId, RoomAccessCallback callback, string password,
            Dictionary<string, string> properties)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(null, "Not connected");
                return;
            }

            var packet = new RoomAccessRequestPacket
            {
                RoomId = roomId,
                Properties = properties,
                Password = password
            };

            Connection.SendMessage((short) OpCodes.GetRoomAccess, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(null, response.AsString("Unknown Error"));
                    return;
                }

                var access = response.Deserialize(new RoomAccessPacket());

                LastReceivedAccess = access;

                callback.Invoke(access, null);

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