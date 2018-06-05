using System;

namespace SpeedDate.Server
{
    public class PeerSecurityExtension
    {
        public int PermissionLevel;
        public string AesKey;
        public byte[] AesKeyEncrypted;
    }
}