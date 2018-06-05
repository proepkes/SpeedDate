using System.Collections.Generic;
using SpeedDate.Networking;
using SpeedDate.Networking.Utils.IO;

namespace SpeedDate.Packets.Rooms
{
    public class RoomAccessRequestPacket : SerializablePacket
    {
        public int RoomId;
        public string Password = "";
        public Dictionary<string, string> Properties;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(RoomId);
            writer.Write(Password);
            writer.Write(Properties);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            RoomId = reader.ReadInt32();
            Password = reader.ReadString();
            Properties = reader.ReadDictionary();
        }
    }
}