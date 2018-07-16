using System.Linq;
using System.Net.Mail;
using Moq;
using NUnit.Framework;
using SpeedDate.ServerPlugins.Mail;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestMail
    {
        [Test]
        public void ShouldCallSendMail()
        {
            const string receiver = "Speed@Date.com";
            const string subject = "TestSubject";
            const string body = "TestBosy";
            
            SetUp.Server.GetPlugin<MailPlugin>().SendMail(receiver, subject, body);
            
            SetUp.SmtpClientMock.Verify(client => client.SendAsync(It.Is<MailMessage>(message =>
                message.To.FirstOrDefault(address => address.Address.Equals(receiver)) != null &&
                message.Subject.Equals(subject) &&
                message.Body.Equals(body)), 
                string.Empty));
        }
    }
}
