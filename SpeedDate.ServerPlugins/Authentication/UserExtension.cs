using SpeedDate.Interfaces;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Authentication;
using SpeedDate.ServerPlugins.Database.Entities;

namespace SpeedDate.ServerPlugins.Authentication
{
    /// <summary>
    /// Instance of this class will be added to 
    /// extensions of a peer who has logged in 
    /// </summary>
    public class UserExtension
    {
        public IPeer Peer { get; }
        public string Username => AccountData.Username;

        public UserExtension(IPeer peer)
        {
            Peer = peer;
        }

        public AccountInfoPacket CreateInfoPacket()
        {
            return new AccountInfoPacket
            {
                Username = AccountData.Username,
                IsAdmin = AccountData.IsAdmin,
                IsGuest = AccountData.IsGuest,
                Properties = AccountData.Properties
            };
        }

        public void Load(AccountData accountData)
        {
            AccountData = accountData;
        }

        public AccountData AccountData { get; set; }
    }
}