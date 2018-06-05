using System.Collections.Generic;
using SpeedDate.Networking;
using SpeedDate.Networking.Utils.IO;

namespace SpeedDate.Packets.Lobbies
{
    public class LobbyTeamData : SerializablePacket
    {
        public string Name;
        public int MinPlayers;
        public int MaxPlayers;
        public Dictionary<string, string> Properties;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(MinPlayers);
            writer.Write(MaxPlayers);
            writer.WriteDictionary(Properties);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Name = reader.ReadString();
            MinPlayers = reader.ReadInt32();
            MaxPlayers = reader.ReadInt32();
            Properties = reader.ReadDictionary();
        }
    }
}