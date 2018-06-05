using SpeedDate.Networking;
using SpeedDate.Networking.Utils.IO;

namespace SpeedDate.Packets.Rooms
{
    public class RoomAccessValidatePacket : SerializablePacket
    {
        public string Token;
        public int RoomId;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Token);
            writer.Write(RoomId);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Token = reader.ReadString();
            RoomId = reader.ReadInt32();
        }
    }
}