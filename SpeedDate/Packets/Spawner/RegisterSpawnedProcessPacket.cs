using SpeedDate.Networking;
using SpeedDate.Networking.Utils.IO;

namespace SpeedDate.Packets.Spawner
{
    public class RegisterSpawnedProcessPacket : SerializablePacket
    {
        public int SpawnId;
        public string SpawnCode;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(SpawnId);
            writer.Write(SpawnCode);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            SpawnId = reader.ReadInt32();
            SpawnCode = reader.ReadString();
        }
    }
}