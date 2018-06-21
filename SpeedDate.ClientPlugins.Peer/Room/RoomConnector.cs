using SpeedDate.Logging;
using SpeedDate.Packets.Rooms;

namespace SpeedDate.ClientPlugins.Peer.Room
{
    /// <summary>
    ///     Base class for connectors.
    ///     Connectors should provide means for client to connect
    ///     to game server
    /// </summary>
    public abstract class RoomConnector
    {
        /// <summary>
        ///     Latest access data. When switching scenes, if this is set,
        ///     connector should most likely try to use this data to connect to game server
        ///     (if the scene is right)
        /// </summary>
        private static RoomAccessPacket _accessData;

        /// <summary>
        ///     Connector instance
        /// </summary>
        public static RoomConnector Instance;

        protected virtual void Awake()
        {
            Instance = this;
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
        }

        /// <summary>
        ///     Should try to connect to game server with data, provided
        ///     in the access packet
        /// </summary>
        /// <param name="access"></param>
        public abstract void ConnectToGame(RoomAccessPacket access);

        #region Static

        /// <summary>
        ///     Publicly accessible method, which clients should use to connect
        ///     to game servers
        /// </summary>
        /// <param name="packet"></param>
        public static void Connect(RoomAccessPacket packet)
        {
            if (Instance == null)
            {
                Logs.Error("Failed to connect to game server. No Game Connector was found in the scene");
                return;
            }

            // Save the access data
            _accessData = packet;

            Instance.ConnectToGame(packet);
        }

        #endregion
    }
}
