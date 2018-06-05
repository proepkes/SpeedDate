using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Networking;

namespace SpeedDate.ServerPlugins.Lobbies
{
    /// <summary>
    /// Lobby factory implementation, which simply invokes
    /// an anonymous method
    /// </summary>
    class LobbyFactoryAnonymous : ILobbyFactory
    {
        private LobbiesServerPlugin _serverPlugin;
        private readonly LobbyCreationFactory _factory;

        public delegate ILobby LobbyCreationFactory(LobbiesServerPlugin serverPlugin, Dictionary<string, string> properties, IPeer creator);

        public LobbyFactoryAnonymous(string id, LobbiesServerPlugin serverPlugin, LobbyCreationFactory factory)
        {
            Id = id;
            _factory = factory;
            _serverPlugin = serverPlugin;
        }

        public ILobby CreateLobby(Dictionary<string, string> properties, IPeer creator)
        {
            var lobby = _factory.Invoke(_serverPlugin, properties, creator);

            // Add the lobby type if it's not set by the factory method
            if (lobby != null && lobby.Type == null)
                lobby.Type = Id;

            return lobby;
        }

        public string Id { get; private set; }
    }
}


