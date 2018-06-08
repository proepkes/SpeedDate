using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Authentication;

namespace SpeedDate.ServerPlugins.Authentication
{
    /// <summary>
    /// This is an interface of a user extension.
    /// Implementation of this interface will be stored in peer's extensions
    /// after he logs in
    /// </summary>
    public interface IUserExtension
    {
        IPeer Peer { get; }

        string Username { get; }

        AccountInfoPacket CreateInfoPacket();

        void Load(IAccountData accountData);

        IAccountData AccountData { get; set; }
    }
}