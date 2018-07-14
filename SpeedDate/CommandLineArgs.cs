using System;
using System.Linq;

namespace SpeedDate
{
    public static class CommandLineArgs
    {
        private static readonly string[] Args;

        static CommandLineArgs()
        {
            Args = Environment.GetCommandLineArgs();
            
            MasterPort = ExtractValueInt(SpeedDateArgNames.MasterPort, 60125);
            MasterIp = ExtractValue(SpeedDateArgNames.MasterIp);
            MachineIp = ExtractValue(SpeedDateArgNames.MachineIp);

            SpawnId = ExtractValueInt(SpeedDateArgNames.SpawnId);
            AssignedPort = ExtractValueInt(SpeedDateArgNames.AssignedPort);
            SpawnCode = ExtractValue(SpeedDateArgNames.SpawnCode);
            ExecutablePath = ExtractValue(SpeedDateArgNames.ExecutablePath);
            DontSpawnInBatchmode = IsProvided(SpeedDateArgNames.DontSpawnInBatchmode);
            MaxProcesses = ExtractValueInt(SpeedDateArgNames.MaxProcesses, 0);

            LoadScene = ExtractValue(SpeedDateArgNames.LoadScene);

            DbConnectionString = ExtractValue(SpeedDateArgNames.DbConnectionString);

            LobbyId = ExtractValueInt(SpeedDateArgNames.LobbyId);
            WebGl = IsProvided(SpeedDateArgNames.WebGl);
            
        }
        
        /// <summary>
        /// Port, which will be open on the master server
        /// </summary>
        public static int MasterPort { get; }

        /// <summary>
        /// Ip address to the master server
        /// </summary>
        public static string MasterIp { get; }

        /// <summary>
        /// Public ip of the machine, on which the process is running
        /// </summary>
        public static string MachineIp { get; }

        /// <summary>
        /// SpawnId of the spawned process
        /// </summary>
        public static int SpawnId { get; }

        /// <summary>
        /// Port, assigned to the spawned process (most likely a game server)
        /// </summary>
        public static int AssignedPort { get; }

        /// <summary>
        /// Code, which is used to ensure that there's no tampering with 
        /// spawned processes
        /// </summary>
        public static string SpawnCode { get; }

        /// <summary>
        /// Path to the executable (used by the spawner)
        /// </summary>
        public static string ExecutablePath { get; }

        /// <summary>
        /// If true, will make sure that spawned processes are not spawned in batchmode
        /// </summary>
        public static bool DontSpawnInBatchmode { get; }

        /// <summary>
        /// Max number of processes that can be spawned by a spawner
        /// </summary>
        public static int MaxProcesses { get; }

        /// <summary>
        /// Name of the scene to load
        /// </summary>
        public static string LoadScene { get; }

        /// <summary>
        /// Database connection string (user by some of the database implementations)
        /// </summary>
        public static string DbConnectionString { get; }
        
        /// <summary>
        /// LobbyId, which is assigned to a spawned process
        /// </summary>
        public static int LobbyId { get; }

        /// <summary>
        /// If true, it will be considered that we want to start server to
        /// support webgl clients
        /// </summary>
        public static bool WebGl { get; }
        
        public static string ExtractValue(string argName, string defaultValue = null)
        {
            if (!Args.Contains(argName))
                return defaultValue;

            var index = Args.ToList().FindIndex(0, a => a.Equals(argName));
            return Args[index + 1];
        }

        public static int ExtractValueInt(string argName, int defaultValue = -1)
        {
            var number = ExtractValue(argName, defaultValue.ToString());
            return Convert.ToInt32(number);
        }

        public static bool IsProvided(string argName)
        {
            return Args.Contains(argName);
        }

        public static class SpeedDateArgNames
        {
            public const string MasterPort = "-sdMasterPort";
            public const string MasterIp = "-sdMasterIp";
            public const string SpawnId = "-sdSpawnId";
            public const string SpawnCode = "-sdSpawnCode";
            public const string AssignedPort = "-sdAssignedPort";
            public const string LoadScene = "-sdLoadScene";
            public const string MachineIp = "-sdMachineIp";
            public const string ExecutablePath = "-sdExe";
            public const string DbConnectionString = "-sdDbConnectionString";
            public const string LobbyId = "-sdLobbyId";
            public const string DontSpawnInBatchmode = "-sdDontSpawnInBatchmode";
            public const string MaxProcesses = "-sdMaxProcesses";
            public const string WebGl = "-sdWebgl";
        }
    }
}
