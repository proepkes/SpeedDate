using System.Collections.Generic;
using System.Linq;
using SpeedDate.Interfaces;
using SpeedDate.Interfaces.Network;
using SpeedDate.Interfaces.Plugins;
using SpeedDate.Network;
using SpeedDate.Packets.Matchmaking;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Lobbies;
using SpeedDate.ServerPlugins.Rooms;

namespace SpeedDate.ServerPlugins.Matchmaker
{
    class MatchmakerPlugin : ServerPluginBase
    {
        protected readonly HashSet<IGamesProvider> GameProviders;


        public MatchmakerPlugin(IServer server) : base(server)
        {
            GameProviders = new HashSet<IGamesProvider>();

            // Add handlers
            Server.SetHandler((short)OpCodes.FindGames, HandleFindGames);

        }

        public override void Loaded(IPluginProvider pluginProvider)
        {
            AddProvider(pluginProvider.Get<RoomsPlugin>());
            AddProvider(pluginProvider.Get<LobbiesPlugin>());
        }

        public void AddProvider(IGamesProvider provider)
        {
            GameProviders.Add(provider);
        }

        private void HandleFindGames(IIncommingMessage message)
        {
            var list = new List<GameInfoPacket>();

            var filters = new Dictionary<string, string>().FromBytes(message.AsBytes());

            foreach (var provider in GameProviders)
            {
                list.AddRange(provider.GetPublicGames(message.Peer, filters));
            }

            // Convert to generic list and serialize to bytes
            var bytes = list.Select(l => (ISerializablePacket)l).ToBytes();

            message.Respond(bytes, ResponseStatus.Success);
        }
    }
}