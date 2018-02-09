using System;
using System.Configuration;
using System.Net.Mail;

namespace OnTime_Cover.extras
{
    class EmailService : IEmailInterface
    {
        public bool SendEmail(string to, string from, string @subject, string @body)
        {
            var message = new MailMessage(from, to, subject, body) { IsBodyHtml = true };
            var client = new SmtpClient(ConfigurationManager.AppSettings["SmtpClientServer"], UInt16.Parse(ConfigurationManager.AppSettings["SmtpClientPort"])); //todo get client from config file.
            message.CC.Add(new MailAddress(ConfigurationManager.AppSettings["CarbonCopyEmail"]));

            try
            {
                client.Send(message);
            }
            catch (Exception) //todo hmm...
            {
                return false;
            }

            return true;
        }

        public string GenerateEmail() //todo ...
        {
            return null;
        }
    }
}
