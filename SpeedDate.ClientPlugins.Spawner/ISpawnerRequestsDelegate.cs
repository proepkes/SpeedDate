using System.Collections.Generic;
using SpeedDate.Configuration;
using SpeedDate.Network.Interfaces;
using SpeedDate.Packets.Spawner;

namespace SpeedDate.ClientPlugins.Spawner
{
    public interface ISpawnerRequestsDelegate
    {
        void HandleSpawnRequest(IIncommingMessage message, SpawnRequestPacket data);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Kills a previously requested spawn </summary>
        ///
        /// <param name="spawnId">  Identifier for the spawn retrieved from the Spawnrequestpacket.SpawnId-field. </param>
        ///
        /// <returns>   True if the spawn with <paramref name="spawnId "/> was killed, false if it fails. </returns>
        ///-------------------------------------------------------------------------------------------------
        bool HandleKillRequest(int spawnId);
    }
}