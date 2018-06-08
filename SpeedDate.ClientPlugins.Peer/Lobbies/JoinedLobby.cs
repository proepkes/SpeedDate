using System;
using System.Collections.Generic;
using SpeedDate.ClientPlugins.Peer.Rooms;
using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Common;
using SpeedDate.Packets.Lobbies;

namespace SpeedDate.ClientPlugins.Peer.Lobbies
{
    /// <summary>
    ///     Represents a joined lobby. When player joins a lobby,
    ///     an instance of this class is created. It acts as a convenient way
    ///     to manage lobby state from player perspective
    /// </summary>
    public class JoinedLobby
    {
        public delegate void LobbyPropertyChangeHandler(string property, string key);

        public delegate void PlayerPropertyChangedHandler(LobbyMemberData member, string propertyKey,
            string propertyValue);

        private readonly IClientSocket _connection;

        private readonly LobbyPlugin _lobbyServer;

        public LobbyDataPacket Data { get; }

        public Dictionary<string, string> Properties { get; }
        public Dictionary<string, LobbyMemberData> Members { get; }
        public Dictionary<string, LobbyTeamData> Teams { get; }

        public ILobbyListener Listener { get; private set; }

        public string LobbyName => Data.LobbyName;

        public int Id => Data.LobbyId;

        public LobbyState State => Data.LobbyState;

        public bool HasLeft { get; private set; }

        public JoinedLobby(LobbyPlugin owner, LobbyDataPacket data, IClientSocket connection)
        {
            Data = data;
            _connection = connection;
            connection.SetHandler((short) OpCodes.LobbyMemberPropertyChanged, HandleMemberPropertyChanged);
            connection.SetHandler((short) OpCodes.LeftLobby, HandleLeftLobbyMsg);
            connection.SetHandler((short) OpCodes.LobbyChatMessage, HandleLobbyChatMessageMsg);
            connection.SetHandler((short) OpCodes.LobbyMemberJoined, HandleLobbyMemberJoinedMsg);
            connection.SetHandler((short) OpCodes.LobbyMemberLeft, HandleLobbyMemberLeftMsg);
            connection.SetHandler((short) OpCodes.LobbyStateChange, HandleLobbyStateChangeMsg);
            connection.SetHandler((short) OpCodes.LobbyStatusTextChange, HandleLobbyStatusTextChangeMsg);
            connection.SetHandler((short) OpCodes.LobbyMemberChangedTeam, HandlePlayerTeamChangeMsg);
            connection.SetHandler((short) OpCodes.LobbyMemberReadyStatusChange, HandleLobbyMemberReadyStatusChangeMsg);
            connection.SetHandler((short) OpCodes.LobbyMasterChange, HandleLobbyMasterChangeMsg);
            connection.SetHandler((short) OpCodes.LobbyPropertyChanged, HandleLobbyPropertyChanged);

            Properties = data.LobbyProperties;
            Members = data.Players;
            Teams = data.Teams;

            _lobbyServer = owner;
        }

        /// <summary>
        ///     Leaves this lobby
        /// </summary>
        public void Leave()
        {
            _lobbyServer.LeaveLobby(Id, () => { });
        }

        /// <summary>
        ///     Leaves this lobby
        /// </summary>
        /// <param name="callback"></param>
        public void Leave(Action callback)
        {
            _lobbyServer.LeaveLobby(Id, callback);
        }

        /// <summary>
        ///     Sets a lobby property to a specified value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetLobbyProperty(string key, string value)
        {
            SetLobbyProperty(key, value, (successful, error) => { });
        }

        /// <summary>
        ///     Sets a lobby property to a specified value
        /// </summary>
        public void SetLobbyProperty(string key, string value, SuccessCallback callback)
        {
            var data = new Dictionary<string, string>
            {
                {key, value}
            };

            _lobbyServer.SetLobbyProperties(Id, data, callback);
        }

        /// <summary>
        ///     Sets a lobby properties to values, provided within a dictionary
        /// </summary>
        public void SetLobbyProperties(Dictionary<string, string> properties, SuccessCallback callback)
        {
            _lobbyServer.SetLobbyProperties(Id, properties, callback);
        }

        /// <summary>
        ///     Sets current player's properties
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetMyProperty(string key, string value)
        {
            SetMyProperty(key, value, (successful, error) => { });
        }


        /// <summary>
        ///     Sets current player's properties
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="callback"></param>
        public void SetMyProperty(string key, string value, SuccessCallback callback)
        {
            var data = new Dictionary<string, string>
            {
                {key, value}
            };

            _lobbyServer.SetMyProperties(data, callback);
        }

        /// <summary>
        ///     Sets current player's properties
        /// </summary>
        public void SetMyProperties(Dictionary<string, string> properties, SuccessCallback callback)
        {
            _lobbyServer.SetMyProperties(properties, callback);
        }

        /// <summary>
        ///     Sets current player's ready status
        /// </summary>
        /// <param name="isReady"></param>
        public void SetReadyStatus(bool isReady)
        {
            _lobbyServer.SetReadyStatus(isReady, (successful, error) => { }, _connection);
        }

        /// <summary>
        ///     Sets current player's ready status
        /// </summary>
        public void SetReadyStatus(bool isReady, SuccessCallback callback)
        {
            _lobbyServer.SetReadyStatus(isReady, callback, _connection);
        }

        /// <summary>
        ///     Sets a lobby event listener
        /// </summary>
        /// <param name="listener"></param>
        public void SetListener(ILobbyListener listener)
        {
            Listener = listener;

            if (listener != null)
                Listener.Initialize(this);
        }

        /// <summary>
        ///     Sends a lobby chat message
        /// </summary>
        /// <param name="message"></param>
        public void SendChatMessage(string message)
        {
            _lobbyServer.SendChatMessage(message);
        }

        /// <summary>
        ///     Switches current user to another team (if available)
        /// </summary>
        /// <param name="teamName"></param>
        /// <param name="callback"></param>
        public void JoinTeam(string teamName, SuccessCallback callback)
        {
            _lobbyServer.JoinTeam(Id, teamName, callback);
        }

        /// <summary>
        ///     Sends a request to server to start a match
        /// </summary>
        /// <param name="callback"></param>
        public void StartGame(SuccessCallback callback)
        {
            _lobbyServer.StartGame(callback);
        }

        /// <summary>
        ///     Retrieves an access to room, which is assigned to this lobby
        /// </summary>
        /// <param name="callback"></param>
        public void GetLobbyRoomAccess(RoomAccessCallback callback)
        {
            _lobbyServer.GetLobbyRoomAccess(new Dictionary<string, string>(), callback);
        }

        /// <summary>
        ///     Retrieves an access to room, which is assigned to this lobby
        /// </summary>
        public void GetLobbyRoomAccess(Dictionary<string, string> properties, RoomAccessCallback callback)
        {
            _lobbyServer.GetLobbyRoomAccess(properties, callback);
        }

        #region Handlers

        private void HandleMemberPropertyChanged(IIncommingMessage message)
        {
            var data = message.Deserialize(new LobbyMemberPropChangePacket());

            if (Id != data.LobbyId)
                return;

            Members.TryGetValue(data.Username, out var member);

            if (member == null)
                return;

            member.Properties[data.Property] = data.Value;

            Listener?.OnMemberPropertyChanged(member, data.Property, data.Value);
        }

        private void HandleLeftLobbyMsg(IIncommingMessage message)
        {
            var id = message.AsInt();

            // Check the id in case there's something wrong with message order
            if (Id != id)
                return;

            HasLeft = true;

            if (Listener != null)
                Listener.OnLobbyLeft();
        }

        private void HandleLobbyChatMessageMsg(IIncommingMessage message)
        {
            var msg = message.Deserialize(new LobbyChatPacket());

            if (Listener != null)
                Listener.OnChatMessageReceived(msg);
        }

        private void HandleLobbyMemberLeftMsg(IIncommingMessage message)
        {
            var username = message.AsString();

            LobbyMemberData member;
            Members.TryGetValue(username, out member);

            if (member == null)
                return;

            Listener?.OnMemberLeft(member);
        }

        private void HandleLobbyMemberJoinedMsg(IIncommingMessage message)
        {
            var data = message.Deserialize(new LobbyMemberData());
            Members[data.Username] = data;

            Listener?.OnMemberJoined(data);
        }

        private void HandleLobbyMasterChangeMsg(IIncommingMessage message)
        {
            var masterUsername = message.AsString();

            Data.GameMaster = masterUsername;

            Listener?.OnMasterChanged(masterUsername);
        }

        private void HandleLobbyMemberReadyStatusChangeMsg(IIncommingMessage message)
        {
            var data = message.Deserialize(new StringPairPacket());

            Members.TryGetValue(data.A, out var member);

            if (member == null)
                return;

            member.IsReady = bool.Parse(data.B);

            Listener?.OnMemberReadyStatusChanged(member, member.IsReady);
        }

        private void HandlePlayerTeamChangeMsg(IIncommingMessage message)
        {
            var data = message.Deserialize(new StringPairPacket());

            Members.TryGetValue(data.A, out var member);

            if (member == null)
                return;

            Teams.TryGetValue(data.B, out var newTeam);

            if (newTeam == null)
                return;

            member.Team = newTeam.Name;

            Listener?.OnMemberTeamChanged(member, newTeam);
        }

        private void HandleLobbyStatusTextChangeMsg(IIncommingMessage message)
        {
            var text = message.AsString();

            Data.StatusText = text;

            Listener?.OnLobbyStatusTextChanged(text);
        }

        private void HandleLobbyStateChangeMsg(IIncommingMessage message)
        {
            var newState = (LobbyState) message.AsInt();

            Data.LobbyState = newState;

            Listener?.OnLobbyStateChange(newState);
        }

        private void HandleLobbyPropertyChanged(IIncommingMessage message)
        {
            var data = message.Deserialize(new StringPairPacket());
            Properties[data.A] = data.B;

            Listener?.OnLobbyPropertyChanged(data.A, data.B);
        }

        #endregion
    }
}