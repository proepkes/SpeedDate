using System;
using SpeedDate.Network.LiteNetLib;

namespace SpeedDate.Network.Interfaces
{
    public delegate void IncommingMessageHandler(IIncommingMessage message);

    public delegate void ResponseCallback(ResponseStatus status, IIncommingMessage response);

    /// <summary>
    ///     Represents connection peer
    /// </summary>
    public interface IPeer : IMsgDispatcher
    {
        long ConnectId { get; }

        ConnectionState ConnectionState { get; }
        /// <summary>
        ///     Invoked when peer disconnects
        /// </summary>
        event PeerActionHandler Disconnected;

        /// <summary>
        ///     Invoked when peer receives a message
        /// </summary>
        event Action<IIncommingMessage> MessageReceived;
        
        /// <summary>
        ///     Force disconnect
        /// </summary>
        /// <param name="reason"></param>
        void Disconnect(string reason);

        /// <summary>
        ///     Stores a property into peer
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        void SetProperty(int id, object data);

        /// <summary>
        ///     Retrieves a property from the peer
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        object GetProperty(int id);

        /// <summary>
        ///     Retrieves a property from the peer, and if it's not found,
        ///     retrieves a default value
        /// </summary>
        /// <param name="id"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        object GetProperty(int id, object defaultValue);

        /// <summary>
        /// Adds an extension to this peer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="extension"></param>
        T AddExtension<T>(T extension);

        /// <summary>
        /// Retrieves an extension of this peer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetExtension<T>();

        bool HasExtension<T>();
    }
}
