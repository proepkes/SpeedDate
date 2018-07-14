using SpeedDate.Network;
using SpeedDate.Network.Utils.IO;

namespace SpeedDate.Packets.Rooms
{
    public class UsernameAndPeerIdPacket : SerializablePacket
    {
        public string Username = "";
        public long PeerId;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Username);
            writer.Write(PeerId);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Username = reader.ReadString();
            PeerId = reader.ReadInt64();
        }

        public override string ToString()
        {
            return $"[Username: {Username}, Peer ID: {PeerId}]";
        }
    }
}
