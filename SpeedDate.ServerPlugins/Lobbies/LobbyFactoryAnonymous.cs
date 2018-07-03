using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;

namespace SpeedDate.ServerPlugins.Lobbies
{
    /// <summary>
    /// Lobby factory implementation, which simply invokes
    /// an anonymous method
    /// </summary>
    public class LobbyFactoryAnonymous : ILobbyFactory
    {
        private readonly LobbiesPlugin _plugin;
        private readonly LobbyCreationFactory _factory;

        public delegate ILobby LobbyCreationFactory(LobbiesPlugin plugin, Dictionary<string, string> properties, IPeer creator);

        public LobbyFactoryAnonymous(string id, LobbiesPlugin plugin, LobbyCreationFactory factory)
        {
            Id = id;
            _factory = factory;
            _plugin = plugin;
        }

        public ILobby CreateLobby(Dictionary<string, string> properties, IPeer creator)
        {
            var lobby = _factory.Invoke(_plugin, properties, creator);

            // Add the lobby type if it's not set by the factory method
            if (lobby != null && lobby.Type == null)
                lobby.Type = Id;

            return lobby;
        }

        public string Id { get; private set; }
    }
}


