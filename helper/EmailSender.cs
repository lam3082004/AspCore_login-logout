using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace api_management.helper
{
    public class EmailSender
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromEmail;

        public EmailSender(IConfiguration configuration)
        {
            _smtpServer = configuration["EmailSettings:SmtpServer"];
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"]);
            _smtpUser = configuration["EmailSettings:SmtpUser"];
            _smtpPass = configuration["EmailSettings:SmtpPass"];
            _fromEmail = configuration["EmailSettings:FromEmail"];
        }

        public async Task SendVerificationEmailAsync(string email, string link, bool check)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Your App Name", _fromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = "Xác minh tài khoản của bạn";

                var bodyBuilder1 = new BodyBuilder
                {
                    HtmlBody = $@"<h2>Xác minh tài khoản</h2>
                              <p>Vui lòng nhấp vào liên kết dưới đây để xác minh email của bạn:</p>
                              <p><a href='{link}'>Xác minh ngay</a></p>
                              <p>Nếu bạn không yêu cầu đăng ký, vui lòng bỏ qua email này.</p>"
                };
                var bodyBuilder2 = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; line-height: 1.6; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #333;'>Yêu cầu đổi mật khẩu</h2>
                            <p>Chúng tôi nhận được yêu cầu đổi mật khẩu cho tài khoản của bạn.</p>
                            <p>Vui lòng nhấp vào liên kết dưới đây để đặt lại mật khẩu:</p>
                            <p>
                                <a href='{link}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Đổi mật khẩu</a>
                            </p>
                            <p>Nếu bạn không yêu cầu đổi mật khẩu, hãy bỏ qua email này. Mật khẩu của bạn sẽ không bị thay đổi.</p>
                            <hr>
                            <p style='font-size: 12px; color: #888;'>Email này được gửi tự động. Vui lòng không trả lời lại.</p>
                        </div>
                    "
                };
                if (check)
                    message.Body = bodyBuilder1.ToMessageBody();
                else
                    message.Body = bodyBuilder2.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpUser, _smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Gửi email thất bại: {ex.Message}");
            }
        }
    }
}