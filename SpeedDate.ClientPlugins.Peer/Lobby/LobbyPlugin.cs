using System;
using System.Collections.Generic;
using SpeedDate.ClientPlugins.Peer.Room;
using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Lobbies;
using SpeedDate.Packets.Rooms;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.ClientPlugins.Peer.Lobby
{
    public class LobbyPlugin : SpeedDateClientPlugin
    {
        public delegate void JoinLobbyCallback(JoinedLobby lobby);
        public delegate void CreateLobbyCallback(int lobbyId);

        /// <summary>
        /// Invoked, when user joins a lobby
        /// </summary>
        public event Action<JoinedLobby> LobbyJoined;

        /// <summary>
        /// Instance of a lobby that was joined the last
        /// </summary>
        public JoinedLobby LastJoinedLobby;

        [Inject] private RoomPlugin _roomPlugin;

        /// <summary>
        /// Sends a request to create a lobby and joins it
        /// </summary>
        public void CreateAndJoin(string factory, Dictionary<string, string> properties, 
            JoinLobbyCallback callback, ErrorCallback errorCallback)
        {
            CreateLobby(factory, properties, id =>
            {
                JoinLobby(id, callback.Invoke, error =>
                {
                    errorCallback.Invoke("Failed to join the lobby: " + error);
                });
            }, error => errorCallback.Invoke("Failed to create lobby: " + error));
        }

        /// <summary>
        /// Sends a request to create a lobby, using a specified factory
        /// </summary>
        public void CreateLobby(string factory, Dictionary<string, string> properties, 
            CreateLobbyCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected");   
                return;
            }

            properties[OptionKeys.LobbyFactoryId] = factory;

            Client.SendMessage((ushort) OpCodes.CreateLobby, properties.ToBytes(), (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                var lobbyId = response.AsInt();

                callback.Invoke(lobbyId);
            });
        }

        /// <summary>
        /// Sends a request to join a lobby
        /// </summary>
        public void JoinLobby(int lobbyId, JoinLobbyCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            // Send the message
            Client.SendMessage((ushort) OpCodes.JoinLobby, lobbyId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown Error"));
                    return;
                }

                var data = response.Deserialize<LobbyDataPacket>();

                var joinedLobby = new JoinedLobby(this, data, Client);

                LastJoinedLobby = joinedLobby;

                callback.Invoke(joinedLobby);

                LobbyJoined?.Invoke(joinedLobby);
            });
        }

        /// <summary>
        /// Sends a request to leave a lobby
        /// </summary>
        public void LeaveLobby(int lobbyId, Action callback, ErrorCallback errorCallback)
        {
            Client.SendMessage((ushort)OpCodes.LeaveLobby, lobbyId, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                    errorCallback.Invoke(response.AsString("Something went wrong when trying to leave a lobby"));
                else
                    callback.Invoke();
            });
        }

        /// <summary>
        /// Sets a ready status of current player
        /// </summary>
        public void SetReadyStatus(bool isReady, SuccessCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            Client.SendMessage((ushort) OpCodes.LobbySetReady, isReady ? 1 : 0, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke();
            });
        }

        /// <summary>
        /// Sets lobby properties of a specified lobby id
        /// </summary>
        public void SetLobbyProperties(int lobbyId, Dictionary<string, string> properties,
            SuccessCallback callback, ErrorCallback errorCallback)
        {
            var packet = new LobbyPropertiesSetPacket
            {
                LobbyId = lobbyId,
                Properties = properties
            };

            Client.SendMessage((ushort) OpCodes.SetLobbyProperties, packet, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke();
            });
        }

        /// <summary>
        /// Sets lobby user properties (current player sets his own properties,
        ///  which can be accessed by game server and etc.)
        /// </summary>
        public void SetMyProperties(Dictionary<string, string> properties,
            SuccessCallback callback, ErrorCallback errorCallback)
        {
            Client.SendMessage((ushort)OpCodes.SetMyLobbyProperties, properties.ToBytes(),
                (status, response) =>
                {
                    if (status != ResponseStatus.Success)
                    {
                        errorCallback.Invoke(response.AsString("unknown error"));
                        return;
                    }

                    callback.Invoke();
                });
        }

        /// <summary>
        /// Current player sends a request to join a team
        /// </summary>
        public void JoinTeam(int lobbyId, string teamName, SuccessCallback callback, ErrorCallback errorCallback)
        {
            var packet = new LobbyJoinTeamPacket()
            {
                LobbyId = lobbyId,
                TeamName = teamName
            };

            Client.SendMessage((ushort)OpCodes.JoinLobbyTeam, packet,
                (status, response) =>
                {
                    if (status != ResponseStatus.Success)
                    {
                        errorCallback.Invoke(response.AsString("unknown error"));
                        return;
                    }

                    callback.Invoke();
                });
        }


        /// <summary>
        /// Current player sends a chat message to lobby
        /// </summary>
        public void SendChatMessage(string message)
        {
            Client.SendMessage((ushort) OpCodes.LobbySendChatMessage, message);
        }

        /// <summary>
        /// Sends a request to start a game
        /// </summary>
        public void StartGame(SuccessCallback callback, ErrorCallback errorCallback)
        {
            Client.SendMessage((ushort) OpCodes.LobbyStartGame, (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown error"));
                    return;
                }

                callback.Invoke();
            });
        }

        /// <summary>
        /// Sends a request to get access to room, which is assigned to this lobby
        /// </summary>
        public void GetLobbyRoomAccess(Dictionary<string, string> properties, RoomAccessCallback callback, ErrorCallback errorCallback)
        {
            if (!Client.IsConnected)
            {
                errorCallback.Invoke("Not connected");
                return;
            }

            Client.SendMessage((ushort)OpCodes.GetLobbyRoomAccess, properties.ToBytes(), (status, response) =>
            {
                if (status != ResponseStatus.Success)
                {
                    errorCallback.Invoke(response.AsString("Unknown Error"));
                    return;
                }

                var access = response.Deserialize<RoomAccessPacket>();

                _roomPlugin.TriggerAccessReceivedEvent(access);

                callback.Invoke(access);
            });
        }
    }
}
