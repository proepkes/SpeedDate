using SpeedDate.Configuration;

namespace SpeedDate.ServerPlugins.Mail
{
    class MailConfig : IConfig
    {
        public string SmtpHost { get; set; } = "relay.unc.edu";
        public int SmtpPort { get; set; } = 25;
        public string EmailFrom { get; set; } = "speed@date.com";
        public string SenderDisplayName { get; set; } = "SpeedDate MasterServer";
        public string SmtpUsername { get; set; } = "user01";
        public string SmtpPassword { get; set; } = "pass01";
    }
}
