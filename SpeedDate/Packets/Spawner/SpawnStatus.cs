namespace SpeedDate.Packets.Spawner
{
    public enum SpawnStatus
    {
        Killed = -1,

        None,
        InQueue,
        StartingProcess,
        WaitingForProcess,
        ProcessRegistered,
        Finalized
    }
}