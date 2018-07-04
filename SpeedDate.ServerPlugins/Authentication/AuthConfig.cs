using SpeedDate.Configuration;

namespace SpeedDate.ServerPlugins.Authentication
{
    public class AuthConfig : IConfig
    {
        public bool EnableGuestLogin { get; set; } = true;
        public string GuestPrefix { get; set; } = "Guest-";
        public int PeerDataPermissionsLevel { get; set; }
        public int UsernameMinChars { get; set; } = 3;
        public int UsernameMaxChars { get; set; } = 12;
        public string ConfirmEmailBody { get; set; }
        public string PasswordResetEmailBody { get; set; }
    }
}
