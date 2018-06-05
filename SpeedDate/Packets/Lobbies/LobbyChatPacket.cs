using SpeedDate.Networking;
using SpeedDate.Networking.Utils.IO;

namespace SpeedDate.Packets.Lobbies
{
    /// <summary>
    /// A lobby chat message 
    /// </summary>
    public class LobbyChatPacket : SerializablePacket
    {
        public string Sender = "";
        public string Message = "";
        public bool IsError;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Sender);
            writer.Write(Message);
            writer.Write(IsError);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Sender = reader.ReadString();
            Message = reader.ReadString();
            IsError = reader.ReadBoolean();
        }
    }
}