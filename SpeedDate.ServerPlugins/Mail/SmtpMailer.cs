using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using SpeedDate.Logging;

namespace SpeedDate.ServerPlugins.Mail
{
    public class SmtpMailer : Mailer
    {
        private readonly List<Exception> _sendMailExceptions;
        public string EmailFrom = "YourGame@gmail.com";

        private readonly Logger _logger = LogManager.GetLogger(typeof(SmtpMailer).Name);
        public string SenderDisplayName = "Awesome Game";

        protected SmtpClient SmtpClient;
        public string SmtpHost = "smtp.gmail.com";
        public string SmtpPassword = "password";
        public int SmtpPort = 587;
        public string SmtpUsername = "username@gmail.com";

        public SmtpMailer()
        {
            _sendMailExceptions = new List<Exception>();
            SetupSmtpClient();
        }


        protected virtual void Update()
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

        protected virtual void SetupSmtpClient()
        {
            // Configure mail client
            SmtpClient = new SmtpClient(SmtpHost, SmtpPort)
            {
                Credentials = new NetworkCredential(SmtpUsername, SmtpPassword),
                EnableSsl = true
            };

            // set the network credentials

            SmtpClient.SendCompleted += (sender, args) =>
            {
                if (args.Error != null)
                    lock (_sendMailExceptions)
                    {
                        _sendMailExceptions.Add(args.Error);
                    }
            };

            ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;
        }

        public override bool SendMail(string to, string subject, string body)
        {
            // Create the mail message (from, to, subject, body)
            var mailMessage = new MailMessage {From = new MailAddress(EmailFrom, SenderDisplayName)};
            mailMessage.To.Add(to);

            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true;
            mailMessage.Priority = MailPriority.High;

            // send the mail
            SmtpClient.SendAsync(mailMessage, "");
            return true;
        }
    }
}