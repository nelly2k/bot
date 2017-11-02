using System.Net.Mail;

namespace bot.core
{
    public class MailService
    {
        public void Send(string message)
        {
            using (var client = new SmtpClient("smtp.google.com",25))
            {
                var mes = new MailMessage
                {
                    From = new MailAddress("nelly2k@gmail.com"),
                    Subject = "Bot update",
                    Body = message
                };
                mes.To.Add("nelly2k@outlook.com");
                client.Send(mes);
            }
        }
    }
}