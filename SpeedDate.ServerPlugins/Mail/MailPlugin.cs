using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using SpeedDate.Configuration;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Network.Interfaces;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.Server;
using SpeedDate.ServerPlugins.Authentication;

namespace SpeedDate.ServerPlugins.Mail
{
    public sealed class MailPlugin : SpeedDateServerPlugin, IUpdatable
    {
        [Inject] private ILogger _logger;
        [Inject] private MailConfig config;
        private readonly List<Exception> _sendMailExceptions = new List<Exception>();
        private SmtpClient _smtpClient;

        public override void Loaded(IPluginProvider pluginProvider)
        {
            base.Loaded(pluginProvider);
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
