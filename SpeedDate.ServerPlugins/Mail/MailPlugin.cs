using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using SpeedDate.Interfaces.Network;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.ServerPlugins.Authentication;

namespace SpeedDate.ServerPlugins.Mail
{
    public sealed class MailPlugin : ServerPluginBase, IUpdatable
    {
        private readonly ILogger _logger;
        private readonly List<Exception> _sendMailExceptions;
        private SmtpClient _smtpClient;


        public MailPlugin(IServer server, ILogger logger) : base(server)
        {
            _logger = logger;
            _sendMailExceptions = new List<Exception>();
            SetupSmtpClient();
            AppUpdater.Instance.Add(this);
        }

        public void Update()
        {
            // Log errors for any exceptions that might have occured
            // when sending mail
            if (_sendMailExceptions.Count > 0)
                lock (_sendMailExceptions)
                {
                    foreach (var exception in _sendMailExceptions) _logger.Error(exception);

                    _sendMailExceptions.Clear();
                }
        }

        private void SetupSmtpClient()
        {
            var config = SpeedDateConfig.GetPluginConfig<MailConfig>();
            // Configure mail client
            _smtpClient = new SmtpClient(config.SmtpHost, config.SmtpPort)
            {
                Credentials = new NetworkCredential(config.SmtpUsername, config.SmtpPassword),
                EnableSsl = true
            };

            // set the network credentials

            _smtpClient.SendCompleted += (sender, args) =>
            {
                if (args.Error != null)
                    lock (_sendMailExceptions)
                    {
                        _sendMailExceptions.Add(args.Error);
                    }
            };

            ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
        }

        public bool SendMail(string to, string subject, string body)
        {
            var config = SpeedDateConfig.GetPluginConfig<MailConfig>();

            // Create the mail message (from, to, subject, body)
            var mailMessage = new MailMessage {From = new MailAddress(config.EmailFrom, config.SenderDisplayName)};
            mailMessage.To.Add(to);

            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true;
            mailMessage.Priority = MailPriority.High;

            // send the mail
            _smtpClient.SendAsync(mailMessage, "");
            return true;
        }
    }
}