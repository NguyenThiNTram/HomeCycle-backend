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

        public async Task SendBusinessApprovalEmailAsync(string toEmail, string businessName)
        {
            var settings = _config.GetSection("EmailSettings");
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(settings["SenderName"], settings["SenderEmail"]));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = "[HomeCycle] Chúc Mừng! Hồ Sơ Doanh Nghiệp Đã Được Phê Duyệt";

            var builder = new BodyBuilder();

            // Thiết kế giao diện thư chúc mừng kèm nút bấm chuyển hướng trực diện (CTA Button)
            var htmlTemplate = new StringBuilder();
            htmlTemplate.Append("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0;'>");
            htmlTemplate.Append($"<h2 style='color: #2e7d32;'>Xin chúc mừng, {businessName}!</h2>");
            htmlTemplate.Append("<p>Hồ sơ năng lực Doanh nghiệp (KYB) của bạn trên hệ thống nền tảng <b>HomeCycle</b> đã được ban quản trị phê duyệt chính thức thành công.</p>");
            htmlTemplate.Append("<p>Hiện tại, tài khoản của bạn đã được kích hoạt đầy đủ các tính năng thương mại nâng cao bao gồm quản lý kho bãi, đăng tin và thực hiện giao dịch tài chính.</p>");
            htmlTemplate.Append("<div style='text-align: center; margin: 30px 0;'>");
            htmlTemplate.Append("<a href='https://homecycle.vn/dashboard' style='background-color: #2e7d32; color: white; padding: 12px 25px; text-decoration: none; font-weight: bold; border-radius: 4px; display: inline-block;'>Truy Cập Dashboard Doanh Nghiệp</a>");
            htmlTemplate.Append("</div>");
            htmlTemplate.Append("<p style='color: #75775; font-size: 12px;'>Nếu nút bấm trên không hoạt động, bạn có thể copy liên kết này vào trình duyệt: https://homecycle.vn/dashboard</p>");
            htmlTemplate.Append("<hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>");
            htmlTemplate.Append("<p style='font-size: 12px; color: #9e9e9e;'>Đây là email tự động từ hệ thống hệ thống, vui lòng không phản hồi lại thư này.</p>");
            htmlTemplate.Append("</div>");

            builder.HtmlBody = htmlTemplate.ToString();
            email.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(settings["MailServer"], int.Parse(settings["MailPort"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(settings["SenderEmail"], settings["SenderPassword"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

  
        public async Task SendBusinessRejectionEmailAsync(string toEmail, string businessName, IEnumerable<string> rejectionReasons)
        {
            var settings = _config.GetSection("EmailSettings");
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(settings["SenderName"], settings["SenderEmail"]));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = "[HomeCycle] Thông Báo: Hồ Sơ Doanh Nghiệp Yêu Cầu Điều Chỉnh";

            var builder = new BodyBuilder();

            // Xây dựng giao diện danh sách lỗi liệt kê tường minh, trực quan
            var htmlTemplate = new StringBuilder();
            htmlTemplate.Append("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0;'>");
            htmlTemplate.Append($"<h2 style='color: #c62828;'>Kính gửi đại diện {businessName},</h2>");
            htmlTemplate.Append("<p>Cảm ơn bạn đã nộp hồ sơ năng lực doanh nghiệp trên <b>HomeCycle</b>. Ban quản trị đã tiến hành rà soát các tài liệu pháp lý đi kèm và nhận thấy một số thông tin hiện tại chưa phù hợp với quy chuẩn nền tảng.</p>");
            htmlTemplate.Append("<p style='font-weight: bold; color: #c62828;'>Chi tiết các lý do yêu cầu sửa đổi từ Moderator:</p>");

            htmlTemplate.Append("<div style='background-color: #ffebee; padding: 15px; border-left: 4px solid #c62828; margin: 15px 0;'>");
            htmlTemplate.Append("<ul style='margin: 0; padding-left: 20px; line-height: 1.6; color: #333;'>");
            foreach (var reason in rejectionReasons)
            {
                htmlTemplate.Append($"<li style='margin-bottom: 8px;'>{reason}</li>");
            }
            htmlTemplate.Append("</ul>");
            htmlTemplate.Append("</div>");

            htmlTemplate.Append("<p>Vui lòng đăng nhập lại vào hệ thống để tiến hành điều chỉnh trực tiếp các lỗi nêu trên và nộp lại hồ sơ (Resubmit) để ban quản trị tiến hành phê duyệt lại.</p>");
            htmlTemplate.Append("<div style='text-align: center; margin: 30px 0;'>");
            htmlTemplate.Append("<a href='https://homecycle.vn/onboarding/registration-detail' style='background-color: #c62828; color: white; padding: 12px 25px; text-decoration: none; font-weight: bold; border-radius: 4px; display: inline-block;'>Chỉnh Sửa & Nộp Lại Hồ Sơ</a>");
            htmlTemplate.Append("</div>");
            htmlTemplate.Append("<hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>");
            htmlTemplate.Append("<p style='font-size: 12px; color: #9e9e9e;'>Mọi thắc mắc vui lòng liên hệ bộ phận CSKH của HomeCycle để được hỗ trợ giải đáp.</p>");
            htmlTemplate.Append("</div>");

            builder.HtmlBody = htmlTemplate.ToString();
            email.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(settings["MailServer"], int.Parse(settings["MailPort"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(settings["SenderEmail"], settings["SenderPassword"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
