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
            //builder.HtmlBody = $"<h3>Mã xác thực OTP của bạn là: <b style='color:red;'>{otpCode}</b></h3>";
            builder.HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                </head>
                <body style='margin: 0; padding: 0; background-color: #f4f6f8; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
                    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background-color: #f4f6f8; padding: 40px 10px;'>
                        <tr>
                            <td align='center'>
                                <table role='presentation' width='100%' style='max-width: 500px; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); border: 1px solid #e5e7eb;'>
                                    <!-- Header -->
                                    <tr>
                                        <td style='background-color: #588b8b; padding: 22px; text-align: center;'>
                                            <h2 style='color: #ffffff; margin: 0; font-size: 22px; font-weight: 600; letter-spacing: 0.5px;'>Your OTP Code</h2>
                                        </td>
                                    </tr>
                                    <!-- Body Content -->
                                    <tr>
                                        <td style='padding: 30px 25px; color: #374151; font-size: 15px; line-height: 1.6;'>
                                            <p style='margin-top: 0; margin-bottom: 16px;'>Hello,</p>
                                            <p style='margin-top: 0; margin-bottom: 20px;'>Your One-Time Password (OTP) for account verification is:</p>
                                
                                            <!-- OTP Box -->
                                            <div style='background-color: #f0f2f5; border-radius: 8px; padding: 18px; text-align: center; margin: 25px 0;'>
                                                <span style='font-size: 34px; font-weight: 700; color: #23b0b0; letter-spacing: 5px; font-family: ""Courier New"", Courier, monospace;'>{otpCode}</span>
                                            </div>
                                
                                            <p style='margin-bottom: 16px;'>This OTP is valid for <strong>5 minutes</strong>. Please do not share this code with anyone.</p>
                                            <p style='margin-bottom: 16px;'>If you didn't request this code, please ignore this email.</p>
                                            <p style='margin-bottom: 0;'>Thank you for using our service!</p>
                                        </td>
                                    </tr>
                                    <!-- Footer -->
                                    <tr>
                                        <td style='background-color: #f9fafb; padding: 16px; text-align: center; font-size: 13px; color: #6b7280; border-top: 1px solid #f3f4f6;'>
                                            © {DateTime.UtcNow.Year} HomeCycle. All rights reserved.
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";
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

            //// Thiết kế giao diện thư chúc mừng kèm nút bấm chuyển hướng trực diện (CTA Button)
            //var htmlTemplate = new StringBuilder();
            //htmlTemplate.Append("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0;'>");
            //htmlTemplate.Append($"<h2 style='color: #2e7d32;'>Xin chúc mừng, {businessName}!</h2>");
            //htmlTemplate.Append("<p>Hồ sơ năng lực Doanh nghiệp (KYB) của bạn trên hệ thống nền tảng <b>HomeCycle</b> đã được ban quản trị phê duyệt chính thức thành công.</p>");
            //htmlTemplate.Append("<p>Hiện tại, tài khoản của bạn đã được kích hoạt đầy đủ các tính năng thương mại nâng cao bao gồm quản lý kho bãi, đăng tin và thực hiện giao dịch tài chính.</p>");
            //htmlTemplate.Append("<div style='text-align: center; margin: 30px 0;'>");
            //htmlTemplate.Append("<a href='https://homecycle.vn/dashboard' style='background-color: #2e7d32; color: white; padding: 12px 25px; text-decoration: none; font-weight: bold; border-radius: 4px; display: inline-block;'>Truy Cập Dashboard Doanh Nghiệp</a>");
            //htmlTemplate.Append("</div>");
            //htmlTemplate.Append("<p style='color: #75775; font-size: 12px;'>Nếu nút bấm trên không hoạt động, bạn có thể copy liên kết này vào trình duyệt: https://homecycle.vn/dashboard</p>");
            //htmlTemplate.Append("<hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>");
            //htmlTemplate.Append("<p style='font-size: 12px; color: #9e9e9e;'>Đây là email tự động từ hệ thống hệ thống, vui lòng không phản hồi lại thư này.</p>");
            //htmlTemplate.Append("</div>");

            //builder.HtmlBody = htmlTemplate.ToString();

            builder.HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                </head>
                <body style='margin: 0; padding: 0; background-color: #f4f6f8; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
                    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background-color: #f4f6f8; padding: 40px 10px;'>
                        <tr>
                            <td align='center'>
                                <table role='presentation' width='100%' style='max-width: 550px; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); border: 1px solid #e5e7eb;'>
                                    <!-- Header -->
                                    <tr>
                                        <td style='background-color: #588b8b; padding: 22px; text-align: center;'>
                                            <h2 style='color: #ffffff; margin: 0; font-size: 20px; font-weight: 600; letter-spacing: 0.5px;'>Hồ Sơ Doanh Nghiệp Đã Được Phê Duyệt</h2>
                                        </td>
                                    </tr>
                                    <!-- Body Content -->
                                    <tr>
                                        <td style='padding: 30px 25px; color: #374151; font-size: 15px; line-height: 1.6;'>
                                            <p style='margin-top: 0; margin-bottom: 16px;'>Xin chúc mừng, <strong>{businessName}</strong>!</p>
                                            <p style='margin-top: 0; margin-bottom: 16px;'>Hồ sơ năng lực Doanh nghiệp (KYB) của bạn trên hệ thống <strong>HomeCycle</strong> đã được Ban quản trị phê duyệt chính thức thành công.</p>
                                
                                            <!-- Highlight Feature Box -->
                                            <div style='background-color: #f0fdf4; border-left: 4px solid #16a34a; border-radius: 4px; padding: 16px; margin: 20px 0; color: #166534; font-size: 14px;'>
                                                Tài khoản của bạn đã được kích hoạt đầy đủ các tính năng thương mại nâng cao: quản lý kho bãi, đăng tin thu mua/bán, thương lượng trực tiếp và thực hiện giao dịch tài chính.
                                            </div>

                                            <!-- CTA Button -->
                                            <div style='text-align: center; margin: 30px 0;'>
                                                <a href='https://homecycle.vn/dashboard' style='background-color: #588b8b; color: #ffffff; padding: 12px 28px; text-decoration: none; font-weight: 600; border-radius: 6px; display: inline-block; font-size: 15px;'>Truy Cập Dashboard Doanh Nghiệp</a>
                                            </div>

                                            <p style='margin-bottom: 16px; font-size: 13px; color: #6b7280;'>Nếu nút bấm trên không hoạt động, bạn có thể copy liên kết này vào trình duyệt:<br/><a href='https://homecycle.vn/dashboard' style='color: #588b8b; word-break: break-all;'>https://homecycle.vn/dashboard</a></p>
                                            <p style='margin-bottom: 0;'>Cảm ơn bạn đã đồng hành cùng HomeCycle!</p>
                                        </td>
                                    </tr>
                                    <!-- Footer -->
                                    <tr>
                                        <td style='background-color: #f9fafb; padding: 16px; text-align: center; font-size: 13px; color: #6b7280; border-top: 1px solid #f3f4f6;'>
                                            © {DateTime.UtcNow.Year} HomeCycle. All rights reserved.
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

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

            //// Xây dựng giao diện danh sách lỗi liệt kê tường minh, trực quan
            //var htmlTemplate = new StringBuilder();
            //htmlTemplate.Append("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0;'>");
            //htmlTemplate.Append($"<h2 style='color: #c62828;'>Kính gửi đại diện {businessName},</h2>");
            //htmlTemplate.Append("<p>Cảm ơn bạn đã nộp hồ sơ năng lực doanh nghiệp trên <b>HomeCycle</b>. Ban quản trị đã tiến hành rà soát các tài liệu pháp lý đi kèm và nhận thấy một số thông tin hiện tại chưa phù hợp với quy chuẩn nền tảng.</p>");
            //htmlTemplate.Append("<p style='font-weight: bold; color: #c62828;'>Chi tiết các lý do yêu cầu sửa đổi từ Moderator:</p>");

            //htmlTemplate.Append("<div style='background-color: #ffebee; padding: 15px; border-left: 4px solid #c62828; margin: 15px 0;'>");
            //htmlTemplate.Append("<ul style='margin: 0; padding-left: 20px; line-height: 1.6; color: #333;'>");
            //foreach (var reason in rejectionReasons)
            //{
            //    htmlTemplate.Append($"<li style='margin-bottom: 8px;'>{reason}</li>");
            //}
            //htmlTemplate.Append("</ul>");
            //htmlTemplate.Append("</div>");

            //htmlTemplate.Append("<p>Vui lòng đăng nhập lại vào hệ thống để tiến hành điều chỉnh trực tiếp các lỗi nêu trên và nộp lại hồ sơ (Resubmit) để ban quản trị tiến hành phê duyệt lại.</p>");
            //htmlTemplate.Append("<div style='text-align: center; margin: 30px 0;'>");
            //htmlTemplate.Append("<a href='https://homecycle.vn/onboarding/registration-detail' style='background-color: #c62828; color: white; padding: 12px 25px; text-decoration: none; font-weight: bold; border-radius: 4px; display: inline-block;'>Chỉnh Sửa & Nộp Lại Hồ Sơ</a>");
            //htmlTemplate.Append("</div>");
            //htmlTemplate.Append("<hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>");
            //htmlTemplate.Append("<p style='font-size: 12px; color: #9e9e9e;'>Mọi thắc mắc vui lòng liên hệ bộ phận CSKH của HomeCycle để được hỗ trợ giải đáp.</p>");
            //htmlTemplate.Append("</div>");

            //builder.HtmlBody = htmlTemplate.ToString();

            var reasonsListBuilder = new StringBuilder();
            foreach (var reason in rejectionReasons)
            {
                reasonsListBuilder.Append($"<li style='margin-bottom: 6px;'>{reason}</li>");
            }

            builder.HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                </head>
                <body style='margin: 0; padding: 0; background-color: #f4f6f8; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
                    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background-color: #f4f6f8; padding: 40px 10px;'>
                        <tr>
                            <td align='center'>
                                <table role='presentation' width='100%' style='max-width: 550px; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); border: 1px solid #e5e7eb;'>
                                    <!-- Header -->
                                    <tr>
                                        <td style='background-color: #588b8b; padding: 22px; text-align: center;'>
                                            <h2 style='color: #ffffff; margin: 0; font-size: 20px; font-weight: 600; letter-spacing: 0.5px;'>Yêu Cầu Điều Chỉnh Hồ Sơ</h2>
                                        </td>
                                    </tr>
                                    <!-- Body Content -->
                                    <tr>
                                        <td style='padding: 30px 25px; color: #374151; font-size: 15px; line-height: 1.6;'>
                                            <p style='margin-top: 0; margin-bottom: 16px;'>Kính gửi đại diện <strong>{businessName}</strong>,</p>
                                            <p style='margin-top: 0; margin-bottom: 16px;'>Cảm ơn bạn đã nộp hồ sơ đăng ký doanh nghiệp trên <strong>HomeCycle</strong>. Ban quản trị đã rà soát tài liệu pháp lý và nhận thấy một số thông tin cần được điều chỉnh:</p>
                                
                                            <!-- Rejection Reasons Container -->
                                            <div style='background-color: #fef2f2; border-left: 4px solid #dc2626; border-radius: 4px; padding: 16px; margin: 20px 0; color: #991b1b;'>
                                                <strong style='display: block; margin-bottom: 8px;'>Lý do yêu cầu sửa đổi:</strong>
                                                <ul style='margin: 0; padding-left: 20px; line-height: 1.5; color: #7f1d1d;'>
                                                    {reasonsListBuilder}
                                                </ul>
                                            </div>

                                            <p style='margin-bottom: 16px;'>Vui lòng đăng nhập lại hệ thống để cập nhật các thông tin trên và nộp lại hồ sơ để được phê duyệt nhanh nhất.</p>

                                            <!-- CTA Button -->
                                            <div style='text-align: center; margin: 30px 0;'>
                                                <a href='https://homecycle.vn/onboarding/registration-detail' style='background-color: #588b8b; color: #ffffff; padding: 12px 28px; text-decoration: none; font-weight: 600; border-radius: 6px; display: inline-block; font-size: 15px;'>Chỉnh Sửa & Nộp Lại Hồ Sơ</a>
                                            </div>

                                            <p style='margin-bottom: 0;'>Mọi thắc mắc vui lòng liên hệ bộ phận CSKH của HomeCycle để được hỗ trợ giải đáp.</p>
                                        </td>
                                    </tr>
                                    <!-- Footer -->
                                    <tr>
                                        <td style='background-color: #f9fafb; padding: 16px; text-align: center; font-size: 13px; color: #6b7280; border-top: 1px solid #f3f4f6;'>
                                            © {DateTime.UtcNow.Year} HomeCycle. All rights reserved.
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

            email.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(settings["MailServer"], int.Parse(settings["MailPort"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(settings["SenderEmail"], settings["SenderPassword"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
