using System.Net;
using System.Net.Mail;

namespace web_phong_kham_tu_nhan.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendResetPasswordAsync(string toEmail, string toName, string newPassword)
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:SmtpUser"];
            var smtpPass = _config["Email:SmtpPass"];
            var fromName = _config["Email:FromName"] ?? "Phòng Khám An Khang";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                  <meta charset='utf-8'>
                  <style>
                    body {{ font-family: 'Segoe UI', Arial, sans-serif; background:#f5f8fc; margin:0; padding:0; }}
                    .wrap {{ max-width:520px; margin:40px auto; background:white;
                             border-radius:16px; overflow:hidden;
                             box-shadow:0 4px 24px rgba(0,0,0,.08); }}
                    .header {{ background:linear-gradient(135deg,#0d6efd,#4dabf7);
                               padding:32px 36px; text-align:center; }}
                    .header h1 {{ color:white; margin:0; font-size:22px; font-weight:700; }}
                    .header p  {{ color:rgba(255,255,255,.85); margin:6px 0 0; font-size:14px; }}
                    .body {{ padding:32px 36px; }}
                    .greeting {{ font-size:16px; color:#1e293b; margin-bottom:20px; }}
                    .pw-box {{ background:#f1f5f9; border-radius:12px; padding:20px;
                               text-align:center; margin:20px 0; border:1px solid #e2e8f0; }}
                    .pw-box .label {{ font-size:13px; color:#64748b; margin-bottom:8px; }}
                    .pw-box .pw {{ font-size:28px; font-weight:700; color:#0d6efd;
                                   letter-spacing:4px; font-family:monospace; }}
                    .warning {{ background:#fffbeb; border-radius:10px; padding:14px 18px;
                                font-size:13px; color:#92400e; border-left:4px solid #f59e0b; }}
                    .warning i {{ font-weight:700; }}
                    .footer {{ padding:20px 36px; background:#f8fafc; text-align:center;
                               font-size:12px; color:#94a3b8; border-top:1px solid #f1f5f9; }}
                  </style>
                </head>
                <body>
                  <div class='wrap'>
                    <div class='header'>
                      <h1>🔐 Đặt lại mật khẩu</h1>
                      <p>Phòng Khám Đa Khoa An Khang</p>
                    </div>
                    <div class='body'>
                      <div class='greeting'>Xin chào <strong>{toName}</strong>,</div>
                      <p style='color:#475569;font-size:14px;line-height:1.6'>
                        Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                        Dưới đây là mật khẩu mới:
                      </p>
                      <div class='pw-box'>
                        <div class='label'>Mật khẩu mới của bạn</div>
                        <div class='pw'>{newPassword}</div>
                      </div>
                      <div class='warning'>
                        <i>⚠️ Lưu ý quan trọng:</i> Hãy đăng nhập và đổi mật khẩu ngay sau khi nhận được email này.
                        Không chia sẻ mật khẩu với bất kỳ ai.
                      </div>
                    </div>
                    <div class='footer'>
                      Email này được gửi tự động. Nếu bạn không yêu cầu đặt lại mật khẩu,
                      vui lòng liên hệ quản trị viên ngay lập tức.
                    </div>
                  </div>
                </body>
                </html>";

            var mail = new MailMessage
            {
                From = new MailAddress(smtpUser, fromName),
                Subject = "Đặt lại mật khẩu - Phòng Khám An Khang",
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(mail);
        }

        public async Task SendWelcomePatientAsync(string toEmail, string toName,
                                           string loginEmail, string password)
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:SmtpUser"];
            var smtpPass = _config["Email:SmtpPass"];
            var fromName = _config["Email:FromName"] ?? "Phòng Khám An Khang";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                  <meta charset='utf-8'>
                  <style>
                    body {{ font-family:'Segoe UI',Arial,sans-serif;background:#f5f8fc;margin:0;padding:0; }}
                    .wrap {{ max-width:520px;margin:40px auto;background:white;
                             border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.08); }}
                    .header {{ background:linear-gradient(135deg,#0d6efd,#4dabf7);padding:32px 36px;text-align:center; }}
                    .header h1 {{ color:white;margin:0;font-size:22px;font-weight:700; }}
                    .header p  {{ color:rgba(255,255,255,.85);margin:6px 0 0;font-size:14px; }}
                    .body {{ padding:32px 36px; }}
                    .greeting {{ font-size:16px;color:#1e293b;margin-bottom:16px; }}
                    .info-box {{ background:#f1f5f9;border-radius:12px;padding:20px;margin:20px 0;border:1px solid #e2e8f0; }}
                    .info-row {{ display:flex;justify-content:space-between;padding:6px 0;
                                 border-bottom:1px solid #e2e8f0;font-size:14px; }}
                    .info-row:last-child {{ border-bottom:none; }}
                    .info-lbl {{ color:#64748b; }}
                    .info-val {{ font-weight:700;color:#1e293b; }}
                    .info-val.blue {{ color:#0d6efd; }}
                    .warning {{ background:#fffbeb;border-radius:10px;padding:14px 18px;
                                font-size:13px;color:#92400e;border-left:4px solid #f59e0b; }}
                    .footer {{ padding:20px 36px;background:#f8fafc;text-align:center;
                               font-size:12px;color:#94a3b8;border-top:1px solid #f1f5f9; }}
                  </style>
                </head>
                <body>
                  <div class='wrap'>
                    <div class='header'>
                      <h1>🏥 Chào mừng đến Phòng Khám An Khang</h1>
                      <p>Tài khoản của bạn đã được tạo thành công</p>
                    </div>
                    <div class='body'>
                      <div class='greeting'>Xin chào <strong>{toName}</strong>,</div>
                      <p style='color:#475569;font-size:14px;line-height:1.6'>
                        Tài khoản bệnh nhân của bạn tại Phòng Khám Đa Khoa An Khang đã được tạo.
                        Dưới đây là thông tin đăng nhập:
                      </p>
                      <div class='info-box'>
                        <div class='info-row'>
                          <span class='info-lbl'>Email đăng nhập</span>
                          <span class='info-val blue'>  {loginEmail}</span>
                        </div>
                        <div class='info-row'>
                          <span class='info-lbl'>Mật khẩu</span>
                          <span class='info-val blue'>  {password}</span>
                        </div>
                      </div>
                      <div class='warning'>
                        <strong>⚠️ Lưu ý:</strong> Hãy đăng nhập và đổi mật khẩu ngay sau khi nhận email này.
                        Với tài khoản này, bạn có thể đặt lịch khám, xem lịch sử khám bệnh và quản lý hồ sơ cá nhân.
                      </div>
                    </div>
                    <div class='footer'>
                      Phòng Khám Đa Khoa An Khang · Hotline: 1900 xxxx<br>
                      Email này được gửi tự động, vui lòng không trả lời.
                    </div>
                  </div>
                </body>
                </html>";

            var mail = new MailMessage
            {
                From = new MailAddress(smtpUser, fromName),
                Subject = "Chào mừng bạn đến Phòng Khám An Khang - Thông tin tài khoản",
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(toEmail, toName));
            await client.SendMailAsync(mail);
        }
    }
}
