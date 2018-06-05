using System;

namespace SpeedDate.ServerPlugins.Mail
{
    public class Mailer
    {
        public virtual bool SendMail(string to, string subject, string body)
        {
            throw new NotImplementedException("SendMail method needs to be overriden");
        }
    }

}

