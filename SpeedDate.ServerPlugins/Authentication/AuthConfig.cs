namespace SpeedDate.ServerPlugins.Authentication
{
    class AuthConfig
    {
        public bool EnableGuestLogin { get; set; }
        public string GuestPrefix { get; set; }
        public int PeerDataPermissionsLevel { get; set; }
        public int UsernameMinChars { get; set; }
        public int UsernameMaxChars { get; set; }
    }
}
