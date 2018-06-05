using SpeedDate.Network;
using SpeedDate.Network.Utils.IO;

namespace SpeedDate.Packets.Common
{
    public class IntPairPacket : SerializablePacket
    {
        public int A;
        public int B;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(A);
            writer.Write(B);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            A = reader.ReadInt32();
            B = reader.ReadInt32();
        }
    }
}