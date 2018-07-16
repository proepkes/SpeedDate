using SpeedDate.Network.Interfaces;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public class LobbyUserExtension
    {
        public IPeer Peer { get; }

        /// <summary>
        /// Lobby, to which current peer belongs
        /// </summary>
        public Lobby CurrentLobby { get; set; }

        public LobbyUserExtension(IPeer peer)
        {
            this.Peer = peer;
        }
    }
}
