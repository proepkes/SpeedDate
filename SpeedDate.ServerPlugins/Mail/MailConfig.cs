﻿using SpeedDate.Configuration;

namespace SpeedDate.ServerPlugins.Mail
{
    [PluginConfiguration(typeof(MailPlugin))]
    class MailConfig
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string EmailFrom { get; set; }
        public string SenderDisplayName { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
    }
}
