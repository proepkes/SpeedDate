using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public class LobbyUserExtension
    {
        public IPeer Peer { get; private set; }

        /// <summary>
        /// Lobby, to which current peer belongs
        /// </summary>
        public ILobby CurrentLobby { get; set; }

        public LobbyUserExtension(IPeer peer)
        {
            this.Peer = peer;
        }
    }
}