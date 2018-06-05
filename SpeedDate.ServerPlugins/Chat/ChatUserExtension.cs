using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Networking;

namespace SpeedDate.ServerPlugins.Chat
{
    public class ChatUserExtension
    {
        public ChatUserExtension(IPeer peer, string username)
        {
            Peer = peer;
            Username = username;
            CurrentChannels = new HashSet<ChatChannel>();
        }

        public HashSet<ChatChannel> CurrentChannels { get; private set; }

        public ChatChannel DefaultChannel { get; set; }

        public string Username { get; private set; }
        public IPeer Peer { get; private set; }

    }
}