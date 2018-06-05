using System;

namespace SpeedDate.ServerPlugins.Authentication
{
    [Serializable]
    internal class PermissionEntry
    {
        public string Key;
        public int PermissionLevel;
    }
}