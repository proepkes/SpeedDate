using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using SpeedDate.Configuration;
using SpeedDate.Interfaces;
using SpeedDate.Logging;
using SpeedDate.Network;
using SpeedDate.Plugin.Interfaces;
using SpeedDate.Server;

namespace SpeedDate.ServerPlugins.Mail
{
    public sealed class MailPlugin : SpeedDateServerPlugin, IUpdatable
    {
        [Inject] private ILogger _logger;
        [Inject] private MailConfig config;
        [Inject] private AppUpdater _updater;
        [Inject] private ISmtpClient _smtpClient;

        private readonly IProducerConsumerCollection<Exception> _sendMailExceptions = new ConcurrentBag<Exception>();

        public override void Loaded(IPluginProvider pluginProvider)
        {
            SetupSmtpClient();
            _updater.Add(this);
        }

        public void SetSmtpClient(ISmtpClient smtpClient)
        {
            _smtpClient = smtpClient;
            SetupSmtpClient();
        }

        public void Update()
        {
            // Log errors for any exceptions that might have occured when sending mail
            if (_sendMailExceptions.Count > 0)
            {
                while (_sendMailExceptions.TryTake(out var e))
                {
                    _logger.Error(e);

                }
            }
        }

        private void SetupSmtpClient()
        {
            _smtpClient.Host = config.SmtpHost;
            _smtpClient.Port = config.SmtpPort;
            _smtpClient.Credentials = new NetworkCredential(config.SmtpUsername, config.SmtpPassword);
            _smtpClient.EnableSsl = true;
            
            _smtpClient.SendCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    _sendMailExceptions.TryAdd(args.Error);
                }
            };

            ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
        }

        public void SendMail(string to, string subject, string body)
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
        }
    }
}
