using System.Collections.Generic;
using SpeedDate.Network;
using SpeedDate.Network.Utils.IO;

namespace SpeedDate.Packets.Lobbies
{
    public class LobbyPropertiesSetPacket : SerializablePacket
    {
        public int LobbyId;
        public Dictionary<string, string> Properties;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(LobbyId);
            writer.Write(Properties);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            LobbyId = reader.ReadInt32();
            Properties = reader.ReadDictionary();
        }
    }
}