using SpeedDate.Configuration;

namespace SpeedDate.ServerPlugins.Authentication
{
    [PluginConfiguration(typeof(AuthPlugin))]
    class AuthConfig
    {
        public bool EnableGuestLogin { get; set; }
        public string GuestPrefix { get; set; }
        public int PeerDataPermissionsLevel { get; set; }
        public int UsernameMinChars { get; set; }
        public int UsernameMaxChars { get; set; }
        public string ConfirmEmailBody { get; set; }
        public string PasswordResetEmailBody { get; set; }
    }
}
