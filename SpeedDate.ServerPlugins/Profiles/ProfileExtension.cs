using SpeedDate.Interfaces;
using SpeedDate.Networking;
using SpeedDate.Packets;

namespace SpeedDate.ServerPlugins.Profiles
{
    public class ProfileExtension
    {
        public string Username { get; private set; }
        public ObservableServerProfile Profile { get; private set; }
        public IPeer Peer { get; private set; }

        public ProfileExtension(ObservableServerProfile profile, IPeer peer)
        {
            Username = profile.Username;
            Profile = profile;
            Peer = peer;
        }
    }
}