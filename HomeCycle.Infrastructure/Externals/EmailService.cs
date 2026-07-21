using HomeCycle.Application.Interfaces.Services.Auths;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var settings = _config.GetSection("EmailSettings");

            Console.WriteLine(settings["MailServer"]);
            Console.WriteLine(settings["MailPort"]);
            Console.WriteLine(settings["SenderEmail"]);

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(settings["SenderName"], settings["SenderEmail"]));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = "Ma OTP xac thuc tai khoan";

            var builder = new BodyBuilder();
            builder.HtmlBody = $"<h3>Mã xác thực OTP của bạn là: <b style='color:red;'>{otpCode}</b></h3>";
            email.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            // Kết nối đến máy chủ SMTP của Gmail
            await smtp.ConnectAsync(settings["MailServer"], int.Parse(settings["MailPort"]), SecureSocketOptions.StartTls);
            // Đăng nhập bằng Email và Mật khẩu ứng dụng
            await smtp.AuthenticateAsync(settings["SenderEmail"], settings["SenderPassword"]);
            // Gửi email
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
