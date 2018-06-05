using SpeedDate.Network;
using SpeedDate.Network.Utils.IO;

namespace SpeedDate.Packets.Rooms
{
    public class SaveRoomOptionsPacket : SerializablePacket
    {
        public int RoomId;
        public RoomOptions Options;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(RoomId);
            writer.Write(Options);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            RoomId = reader.ReadInt32();
            Options = reader.ReadPacket(new RoomOptions());
        }
    }
}