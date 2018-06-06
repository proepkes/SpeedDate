using System;
using System.Linq;

namespace SpeedDate
{
    public static class CommandLineArgs
    {
        private static readonly string[] args;

        public static readonly MsfArgNames Names;

        static CommandLineArgs()
        {
            args = Environment.GetCommandLineArgs() ?? new string[0];

            // Android fix
            Names = new MsfArgNames();

            StartMaster = IsProvided(Names.StartMaster);
            MasterPort = ExtractValueInt(Names.MasterPort, 60125);
            MasterIp = ExtractValue(Names.MasterIp);
            MachineIp = ExtractValue(Names.MachineIp);

            SpawnId = ExtractValueInt(Names.SpawnId);
            AssignedPort = ExtractValueInt(Names.AssignedPort);
            SpawnCode = ExtractValue(Names.SpawnCode);
            ExecutablePath = ExtractValue(Names.ExecutablePath);
            DontSpawnInBatchmode = IsProvided(Names.DontSpawnInBatchmode);
            MaxProcesses = ExtractValueInt(Names.MaxProcesses, 0);

            LoadScene = ExtractValue(Names.LoadScene);

            DbConnectionString = ExtractValue(Names.DbConnectionString);

            LobbyId = ExtractValueInt(Names.LobbyId);
            WebGl = IsProvided(Names.WebGl);
            
        }

        #region Arguments

        /// <summary>
        /// If true, master server should be started
        /// </summary>
        public static bool StartMaster { get; private set; }

        /// <summary>
        /// Port, which will be open on the master server
        /// </summary>
        public static int MasterPort { get; private set; }

        /// <summary>
        /// Ip address to the master server
        /// </summary>
        public static string MasterIp { get; private set; }

        /// <summary>
        /// Public ip of the machine, on which the process is running
        /// </summary>
        public static string MachineIp { get; private set; }

        /// <summary>
        /// SpawnId of the spawned process
        /// </summary>
        public static int SpawnId { get; private set; }

        /// <summary>
        /// Port, assigned to the spawned process (most likely a game server)
        /// </summary>
        public static int AssignedPort { get; private set; }

        /// <summary>
        /// Code, which is used to ensure that there's no tampering with 
        /// spawned processes
        /// </summary>
        public static string SpawnCode { get; private set; }

        /// <summary>
        /// Path to the executable (used by the spawner)
        /// </summary>
        public static string ExecutablePath { get; private set; }

        /// <summary>
        /// If true, will make sure that spawned processes are not spawned in batchmode
        /// </summary>
        public static bool DontSpawnInBatchmode { get; private set; }

        /// <summary>
        /// Max number of processes that can be spawned by a spawner
        /// </summary>
        public static int MaxProcesses { get; private set; }

        /// <summary>
        /// Name of the scene to load
        /// </summary>
        public static string LoadScene { get; private set; }

        /// <summary>
        /// Database connection string (user by some of the database implementations)
        /// </summary>
        public static string DbConnectionString { get; private set; }
        
        /// <summary>
        /// LobbyId, which is assigned to a spawned process
        /// </summary>
        public static int LobbyId { get; private set; }

        /// <summary>
        /// If true, it will be considered that we want to start server to
        /// support webgl clients
        /// </summary>
        public static bool WebGl { get; private set; }

        #endregion

        #region Helper methods

        /// <summary>
        ///     Extracts a value for command line arguments provided
        /// </summary>
        /// <param name="argName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string ExtractValue(string argName, string defaultValue = null)
        {
            if (!args.Contains(argName))
                return defaultValue;

            var index = args.ToList().FindIndex(0, a => a.Equals(argName));
            return args[index + 1];
        }

        public static int ExtractValueInt(string argName, int defaultValue = -1)
        {
            var number = ExtractValue(argName, defaultValue.ToString());
            return Convert.ToInt32(number);
        }

        public static bool IsProvided(string argName)
        {
            return args.Contains(argName);
        }

        #endregion

        public class MsfArgNames
        {
            public string StartMaster => "-msfStartMaster";
            public string MasterPort => "-msfMasterPort";
            public string MasterIp => "-msfMasterIp";

            public string StartSpawner => "-msfStartSpawner";

            public string SpawnId => "-msfSpawnId";
            public string SpawnCode => "-msfSpawnCode";
            public string AssignedPort => "-msfAssignedPort";
            public string LoadScene => "-msfLoadScene";
            public string MachineIp => "-msfMachineIp";
            public string ExecutablePath => "-msfExe";
            public string DbConnectionString => "-msfDbConnectionString";
            public string LobbyId => "-msfLobbyId";
            public string DontSpawnInBatchmode => "-msfDontSpawnInBatchmode";
            public string MaxProcesses => "-msfMaxProcesses";
            public string WebGl => "-msfWebgl";
        }
    }
}