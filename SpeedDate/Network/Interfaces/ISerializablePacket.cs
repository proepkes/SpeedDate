
using SpeedDate.Network.Utils.IO;

namespace SpeedDate.Network.Interfaces
{
    public interface ISerializablePacket
    {
        void ToBinaryWriter(EndianBinaryWriter writer);
        void FromBinaryReader(EndianBinaryReader reader);
        byte[] ToBytes();
    }
}