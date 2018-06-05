using System;

namespace SpeedDate.ServerPlugins.Authentication
{
    internal class PermissionEntry
    {
        public readonly string Key;
        public readonly int PermissionLevel;

        public PermissionEntry(string key, int permissionLevel)
        {
            Key = key;
            PermissionLevel = permissionLevel;
        }
    }
}