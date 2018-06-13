using System.Collections.Generic;

using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Network.LiteNetLib;
using SpeedDate.Packets.Chat;

namespace SpeedDate.ServerPlugins.Chat
{
    public class ChatChannel
    {
        public string Name { get; private set; }

        private readonly Dictionary<string, ChatUserExtension> _users;

        public IEnumerable<ChatUserExtension> Users => _users.Values;

        public ChatChannel(string name)
        {
            Name = name;
            _users = new Dictionary<string, ChatUserExtension>();
        }

        /// <summary>
        /// Returns true, if user successfully joined a channel
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool AddUser(ChatUserExtension user)
        {
            if (!IsUserAllowed(user))
                return false;

            // Add disconnect listener
            user.Peer.Disconnected += OnUserDisconnect;

            // Add user
            _users.Add(user.Username, user);

            // Add channel to users collection
            user.CurrentChannels.Add(this);

            OnJoined(user);
            return true;
        }

        protected virtual void OnJoined(ChatUserExtension newUser)
        {
            var data = new List<string>() {Name, newUser.Username};
            var msg = MessageHelper.Create((ushort) OpCodes.UserJoinedChannel, data.ToBytes());

            foreach (var user in _users.Values)
            {
                if (user != newUser)
                {
                    user.Peer.SendMessage(msg, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        protected virtual void OnLeft(ChatUserExtension removedUser)
        {
            var data = new List<string>() { Name, removedUser.Username };
            var msg = MessageHelper.Create((ushort)OpCodes.UserLeftChannel, data.ToBytes());

            foreach (var user in _users.Values)
            {
                if (user != removedUser)
                {
                    user.Peer.SendMessage(msg, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        protected virtual bool IsUserAllowed(ChatUserExtension user)
        {
            // Can't join if already here
            return !_users.ContainsKey(user.Username);
        }

        /// <summary>
        /// Invoked, when user, who is connected to this channel, leaves
        /// </summary>
        /// <param name="peer"></param>
        protected virtual void OnUserDisconnect(IPeer peer)
        {
            var extension = peer.GetExtension<ChatUserExtension>();

            if (extension == null)
                return;

            RemoveUser(extension);
        }

        public void RemoveUser(ChatUserExtension user)
        {
            // Remove disconnect listener
            user.Peer.Disconnected -= OnUserDisconnect;

            // Remove channel from users collection
            user.CurrentChannels.Remove(this);

            // Remove user
            _users.Remove(user.Username);

            if (user.DefaultChannel == this)
                user.DefaultChannel = null;

            OnLeft(user);
        }

        /// <summary>
        /// Handle messages
        /// </summary>
        public virtual void BroadcastMessage(ChatMessagePacket packet)
        {
            // Override name to be in a "standard" format (uppercase letters and etc.)
            packet.Receiver = Name;

            var msg = MessageHelper.Create((ushort) OpCodes.ChatMessage, packet);

            foreach (var user in _users.Values)
            {
                user.Peer.SendMessage(msg, DeliveryMethod.ReliableUnordered);
            }
        }
    }
}