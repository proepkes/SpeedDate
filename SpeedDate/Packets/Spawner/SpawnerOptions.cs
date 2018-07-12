using System.Collections.Generic;
using System.Linq;
using SpeedDate.Network;
using SpeedDate.Network.Utils.IO;

namespace SpeedDate.Packets.Spawner
{
    public class SpawnerOptions : SerializablePacket
    {
        /// <summary>
        /// Max number of processes that this spawner can handle. If 0 - unlimited
        /// </summary>
        public int MaxProcesses = 0;

        /// <summary>
        /// Region, to which the spawner belongs
        /// </summary>
        public string Region = "International";

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(MaxProcesses);
            writer.Write(Region);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            MaxProcesses = reader.ReadInt32();
            Region = reader.ReadString();
        }

        public override string ToString()
        {
            return $"MaxProcesses: {MaxProcesses}, Region: {Region}";
        }
    }
}
