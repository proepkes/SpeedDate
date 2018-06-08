using System.Collections.Generic;
using SpeedDate.Interfaces;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Chat;

namespace SpeedDate.ClientPlugins.Peer.Chat
{
    public class ChatPlugin : SpeedDateClientPlugin
    {
        public delegate void ChatChannelsCallback(List<string> channels, string error);
        public delegate void ChatUsersCallback(List<string> users, string error);

        public delegate void ChatUserHandler(string channel, string user);
        public delegate void ChatMessageHandler(ChatMessagePacket message);

        /// <summary>
        /// Invoked, when user leaves a channel
        /// </summary>
        public event ChatUserHandler UserLeftChannel;

        /// <summary>
        /// Invoked, when user joins a channel
        /// </summary>
        public event ChatUserHandler UserJoinedChannel;

        /// <summary>
        /// Invoked, when a new message is received
        /// </summary>
        public event ChatMessageHandler MessageReceived;

        public ChatPlugin(IClientSocket connection) : base(connection)
        {
            connection.SetHandler((short)OpCodes.UserJoinedChannel, HandleUserJoinedChannel);
            connection.SetHandler((short)OpCodes.UserLeftChannel, HandleUserLeftChannel);
            connection.SetHandler((short)OpCodes.ChatMessage, HandleChatMessage);
        }


        /// <summary>
        /// Sends a request to set chat username
        /// </summary>
        public void PickUsername(string username, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            Connection.SendMessage((short)OpCodes.PickUsername, username, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Sends a request to join a specified channel
        /// </summary>
        public void JoinChannel(string channel, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            Connection.SendMessage((short) OpCodes.JoinChannel, channel, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }


        /// <summary>
        /// Sends a request to leave a specified channel
        /// </summary>
        public void LeaveChannel(string channel, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            Connection.SendMessage((short)OpCodes.LeaveChannel, channel, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Sets a default channel to the specified channel.
        /// Messages, that have no channel, will be sent to default channel
        /// </summary>
        public void SetDefaultChannel(string channel, SuccessCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(false, "Not connected");
                return;
            }

            Connection.SendMessage((short)OpCodes.SetDefaultChannel, channel, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        /// <summary>
        /// Retrieves a list of channels, which user has joined
        /// </summary>
        public void GetMyChannels(ChatChannelsCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(new List<string>(), "Not connected");
                return;
            }

            Connection.SendMessage((short)OpCodes.GetCurrentChannels, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(new List<string>(), response.AsString("Unknown error"));
                    return;
                }

                var list = new List<string>().FromBytes(response.AsBytes());

                callback.Invoke(list, null);
            });
        }
        
        /// <summary>
        /// Retrieves a list of users in a channel
        /// </summary>
        public void GetUsersInChannel(string channel, ChatUsersCallback callback)
        {
            if (!Connection.IsConnected)
            {
                callback.Invoke(new List<string>(), "Not connected");
                return;
            }

            Connection.SendMessage((short)OpCodes.GetUsersInChannel, channel, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(new List<string>(), response.AsString("Unknown error"));
                    return;
                }

                var list = new List<string>().FromBytes(response.AsBytes());

                callback.Invoke(list, null);
            });
        }

        /// <summary>
        /// Sends a message to default channel
        /// </summary>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        public void SendToDefaultChannel(string message, SuccessCallback callback)
        {
            SendMessage(new ChatMessagePacket()
            {
                Receiver = "",
                Message = message,
                Type = ChatMessagePacket.ChannelMessage
            }, callback);
        }

        /// <summary>
        /// Sends a message to specified channel
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        public void SendChannelMessage(string channel, string message, SuccessCallback callback)
        {
            SendMessage(new ChatMessagePacket()
            {
                Receiver = channel,
                Message = message,
                Type = ChatMessagePacket.ChannelMessage
            }, callback);
        }

        /// <summary>
        /// Sends a private message to specified user
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        public void SendPrivateMessage(string receiver, string message, SuccessCallback callback)
        {
            SendMessage(new ChatMessagePacket()
            {
                Receiver = receiver,
                Message = message,
                Type = ChatMessagePacket.PrivateMessage
            }, callback);
        }

        /// <summary>
        /// Sends a generic message packet to server
        /// </summary>
        public void SendMessage(ChatMessagePacket packet, SuccessCallback callback)
        {
            Connection.SendMessage((short)OpCodes.ChatMessage, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    callback.Invoke(false, response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke(true, null);
            });
        }

        #region Message handlers

        private void HandleChatMessage(IIncommingMessage message)
        {
            var packet = message.Deserialize(new ChatMessagePacket());

            MessageReceived?.Invoke(packet);
        }

        private void HandleUserLeftChannel(IIncommingMessage message)
        {
            var data = new List<string>().FromBytes(message.AsBytes());
            UserLeftChannel?.Invoke(data[0], data[1]);
        }

        private void HandleUserJoinedChannel(IIncommingMessage message)
        {
            var data = new List<string>().FromBytes(message.AsBytes());
            UserJoinedChannel?.Invoke(data[0], data[1]);
        }

        #endregion
    }
}