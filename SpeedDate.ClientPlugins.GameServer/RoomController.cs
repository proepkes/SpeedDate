using System;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Rooms;

namespace SpeedDate.ClientPlugins.GameServer
{
    public delegate void RoomAccessProviderCallback(RoomAccessPacket access);
    public delegate void RoomAccessProvider(UsernameAndPeerIdPacket requester, RoomAccessProviderCallback giveAccess, ErrorCallback errorCallback);

    /// <summary>
    /// Instance of this class will be created when room registration is completed.
    /// It acts as a helpful way to manage a registered room.
    /// </summary>
    public class RoomController
    {
        public readonly IClient Client;

        public int RoomId { get; private set; }
        public RoomOptions Options { get; private set; }

        private RoomAccessProvider _accessProvider;

        public static readonly Logger Logger = LogManager.GetLogger(typeof(RoomController).Name, LogLevel.Warn);

        private readonly RoomsPlugin _roomsPlugin;

        public RoomController(RoomsPlugin owner, int roomId, IClient client, RoomOptions options)
        {
            _roomsPlugin = owner;

            Client = client;
            RoomId = roomId;
            Options = options;

            // Add handlers
            client.SetHandler((ushort) OpCodes.ProvideRoomAccessCheck, HandleProvideRoomAccessCheck);
        }

        /// <summary>
        /// Destroys and unregisters the room
        /// </summary>
        public void Destroy()
        {
            Destroy(() =>
            {
                {
                    Logger.Debug("Unregistered room successfully: " + RoomId);
                }
            }, reason => Logger.Error(reason));
        }

        /// <summary>
        /// Destroys and unregisters the room
        /// </summary>
        public void Destroy(SuccessCallback callback, ErrorCallback errorCallback)
        {
            _roomsPlugin.DestroyRoom(RoomId, callback, errorCallback, Client);
        }

        /// <summary>
        /// Sends current options to master server
        /// </summary>
        public void SaveOptions()
        {
            SaveOptions(Options);
        }

        /// <summary>
        /// Sends new options to master server
        /// </summary>
        public void SaveOptions(RoomOptions options)
        {
            SaveOptions(options, () =>
            {
                Logger.Debug("Room "+ RoomId + " options changed successfully");
                Options = options;
            }, reason =>
            {
                Logger.Error(reason);
            });
        }

        /// <summary>
        /// Sends new options to master server
        /// </summary>
        public void SaveOptions(RoomOptions options, SuccessCallback callback, ErrorCallback errorCallback)
        {
            _roomsPlugin.SaveOptions(RoomId, options, () =>
            {
                Options = options;

                callback.Invoke();

            }, errorCallback.Invoke, Client);
        }

        /// <summary>
        /// Call this, if you want to manually check if peer should receive an access
        /// </summary>
        /// <param name="provider"></param>
        public void SetAccessProvider(RoomAccessProvider provider)
        {
            _accessProvider = provider;
        }

        /// <summary>
        /// Sends the token to "master" server to see if it's valid. If it is -
        /// callback will be invoked with peer id of the user, whos access was confirmed.
        /// This peer id can be used to retrieve users data from master server
        /// </summary>
        /// <param name="token"></param>
        /// <param name="callback"></param>
        public void ValidateAccess(string token, RoomAccessValidateCallback callback, ErrorCallback errorCallback)
        {
            _roomsPlugin.ValidateAccess(RoomId, token, callback, errorCallback);
        }

        public void PlayerLeft(int peerId)
        {
            _roomsPlugin.NotifyPlayerLeft(RoomId, peerId, 
                () =>{ }, 
                reason =>
                {
                    Logger.Error(reason);
                    
                });
        }

        /// <summary>
        /// Default access provider, which always confirms access requests
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="callback"></param>
        public void DefaultAccessProvider(UsernameAndPeerIdPacket requester, RoomAccessProviderCallback callback, ErrorCallback errorCallback)
        {
            callback.Invoke(new RoomAccessPacket
            {
               RoomIp = Options.RoomIp,
               RoomPort = Options.RoomPort,
               Properties = Options.Properties,
               RoomId = RoomId,
               Token = Guid.NewGuid().ToString(),
               SceneName = "SceneName"
            });
        }

        /// <summary>
        /// Makes the room public
        /// </summary>
        public void MakePublic()
        {
            Options.IsPublic = true;
            SaveOptions(Options);
        }

        /// <summary>
        /// Makes the room public
        /// </summary>
        public void MakePublic(Action callback)
        {
            Options.IsPublic = true;
            SaveOptions(Options, callback.Invoke, reason =>
            {
                
            });
        }

        #region Message handlers

        private void HandleProvideRoomAccessCheck(IIncommingMessage message)
        {
            var data = message.Deserialize<RoomAccessProvideCheckPacket>();

            var roomController = _roomsPlugin.GetRoomController(data.RoomId);

            if (roomController == null)
            {
                message.Respond("There's no room controller with room id " + data.RoomId, ResponseStatus.NotHandled);
                return;
            }

            var accessProvider = roomController._accessProvider ?? DefaultAccessProvider;
            var isProviderDone = false;

            var requester = new UsernameAndPeerIdPacket()
            {
                PeerId = data.PeerId,
                Username = data.Username
            };

            // Invoke the access provider
            accessProvider.Invoke(requester, (access) =>
            {
                // In case provider timed out
                if (isProviderDone)
                    return;

                isProviderDone = true;

                message.Respond(access, ResponseStatus.Success);

                if (Logger.IsLogging(LogLevel.Trace))
                    Logger.Trace("Room controller gave address to peer " + data.PeerId + ":" + access);

            }, error =>
            {
                // In case provider timed out
                if (isProviderDone)
                    return;

                isProviderDone = true;
                
                message.Respond(error, ResponseStatus.Failed);
            });

            // Timeout the access provider
            AppTimer.AfterSeconds(_roomsPlugin.AccessProviderTimeout, () =>
            {
                if (!isProviderDone)
                {
                    isProviderDone = true;
                    message.Respond("Timed out", ResponseStatus.Timeout);
                    Logger.Error("Access provider took longer than " + _roomsPlugin.AccessProviderTimeout + " seconds to provide access. " +
                               "If it's intended, increase the threshold at Msf.Server.Rooms.AccessProviderTimeout");
                }
            });
        }

        #endregion
    }
}
