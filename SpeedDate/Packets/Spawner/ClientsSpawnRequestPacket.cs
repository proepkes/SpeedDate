using System.Collections.Generic;
using SpeedDate.Networking;
using SpeedDate.Networking.Utils.IO;

namespace SpeedDate.Packets.Spawner
{
    public class ClientsSpawnRequestPacket : SerializablePacket
    {
        public string Region = "";
        public Dictionary<string, string> Options;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Region);
            writer.Write(Options);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Region = reader.ReadString();
            Options = reader.ReadDictionary();
        }
    }
}