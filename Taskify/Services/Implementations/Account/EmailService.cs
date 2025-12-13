using System.Net;
using System.Net.Mail;

namespace Taskify.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _config.GetSection("EmailSettings");
            var mail = emailSettings["Mail"]; 
            var password = emailSettings["Password"];
            var client = new SmtpClient(emailSettings["Host"], int.Parse(emailSettings["Port"]))
            {
                Credentials = new NetworkCredential(mail, password),
                //EnableSsl = bool.Parse(emailSettings["EnableSSL"])
                EnableSsl = true
            };
            var mailMessage = new MailMessage(mail,toEmail, subject, body)
            {
                IsBodyHtml = true
            };
            await client.SendMailAsync(mailMessage);
        }
    }
}
