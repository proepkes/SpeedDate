using System;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using SpeedDate.Plugin.Interfaces;

namespace SpeedDate.ServerPlugins.Mail
{
    public interface ISmtpClient
    {
        event Action<object, AsyncCompletedEventArgs> SendCompleted;

        ICredentialsByHost Credentials { get; set; }
        bool EnableSsl { get; set; }
        string Host { get; set; }
        int Port { get; set; }

        void SendAsync(MailMessage mailMessage, string userToken);
    }

    public class DotNetSmtpClient : ISmtpClient, IDisposable, IPluginResource<ISmtpClient>
    {
        private readonly SmtpClient _smtpClient;

        public event Action<object, AsyncCompletedEventArgs> SendCompleted;

        public DotNetSmtpClient()
        {
            _smtpClient = new SmtpClient();
            _smtpClient.SendCompleted += OnSendCompleted;
        }
        
        public ICredentialsByHost Credentials
        {
            get => _smtpClient.Credentials;
            set => _smtpClient.Credentials = value;
        }

        public bool EnableSsl
        {
            get => _smtpClient.EnableSsl;
            set => _smtpClient.EnableSsl = value;
        }

        public string Host
        {
            get => _smtpClient.Host;
            set => _smtpClient.Host = value;
        }

        public int Port
        {
            get => _smtpClient.Port;
            set => _smtpClient.Port = value;
        }

        public void SendAsync(MailMessage mailMessage, string userToken)
        {
            _smtpClient.SendAsync(mailMessage, userToken);
        }
        public void Dispose()
        {
            _smtpClient.SendCompleted -= OnSendCompleted;
            _smtpClient?.Dispose();
        }

        private void OnSendCompleted(object o, AsyncCompletedEventArgs a)
        {
            SendCompleted?.Invoke(o, a);
        }
    }
}
