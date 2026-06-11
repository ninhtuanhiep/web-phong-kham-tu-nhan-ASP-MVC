using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services;

namespace web_phong_kham_tu_nhan.Services
{
    /// <summary>
    /// Background service chạy mỗi giờ.
    /// Mỗi lần chạy: tìm tất cả lịch hẹn có AppointmentDate = NGÀY MAI
    /// và TrangThai = 1 (đã xác nhận), gửi email + thông báo in-app cho bệnh nhân.
    /// Tránh gửi trùng bằng cách kiểm tra bảng ThongBaos.
    /// </summary>
    public class NhacLichBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NhacLichBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public NhacLichBackgroundService(IServiceScopeFactory scopeFactory,
                                          ILogger<NhacLichBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NhacLichBackgroundService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await GuiThongBaoNhacLich(); }
                catch (Exception ex) { _logger.LogError(ex, "Lỗi NhacLichBackgroundService."); }
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task GuiThongBaoNhacLich()
        {
            using var scope    = _scopeFactory.CreateScope();
            var context        = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailSvc       = scope.ServiceProvider.GetRequiredService<EmailService>();

            // Lấy lịch hẹn ngày mai đã xác nhận (TrangThai = 1)
            DateTime ngayMai   = DateTime.Today.AddDays(1);
            var lichHenNgayMai = await context.Appointments
                .Include(a => a.BacSi).ThenInclude(b => b!.ChuyenKhoa)
                .Include(a => a.BenhNhan)
                .Where(a => a.AppointmentDate.Date == ngayMai.Date && a.TrangThai == 1)
                .ToListAsync();

            _logger.LogInformation("Tìm thấy {count} lịch hẹn ngày mai cần nhắc.", lichHenNgayMai.Count);

            foreach (var lich in lichHenNgayMai)
            {
                if (lich.BenhNhan == null || lich.BacSi == null) continue;

                // Kiểm tra đã gửi thông báo in-app hôm nay chưa
                bool daGui = await context.ThongBaos.AnyAsync(t =>
                    t.LichHenId    == lich.Id &&
                    t.LoaiThongBao == "NhacLich" &&
                    t.ThoiGianTao  >= DateTime.Today);

                if (daGui) continue;

                // Tìm User của bệnh nhân để lấy email
                var benhNhanUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == lich.BenhNhan.UserId);
                if (benhNhanUser == null) continue;

                string tenBN      = lich.BenhNhan.FullName ?? benhNhanUser.FullName ?? "Bệnh nhân";
                string tenBS      = lich.BacSi.FullName ?? "Bác sĩ";
                string chuyenKhoa = lich.BacSi.ChuyenKhoa?.Name ?? "";
                string ngayKham   = lich.AppointmentDate.ToString("dd/MM/yyyy");
                string gioKham    = lich.TimeSlot ?? "Theo lịch hẹn";

                // 1. Gửi email
                string emailBN = lich.BenhNhan.Email ?? benhNhanUser.Email ?? "";
                if (!string.IsNullOrEmpty(emailBN))
                {
                    try
                    {
                        await emailSvc.SendNhacLichHenAsync(
                            emailBN, tenBN, tenBS, chuyenKhoa, ngayKham, gioKham);
                        _logger.LogInformation("Đã gửi email nhắc lịch cho {TenBN} ({Email})", tenBN, emailBN);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi gửi email nhắc lịch cho {Email}", emailBN);
                    }
                }

                // 2. Tạo thông báo in-app
                context.ThongBaos.Add(new ThongBao
                {
                    NguoiNhanId  = benhNhanUser.Id,
                    LoaiThongBao = "NhacLich",
                    TieuDe       = $"⏰ Nhắc lịch khám ngày {ngayKham}",
                    NoiDung      = $"Bạn có lịch khám với Bác sĩ {tenBS} vào lúc {gioKham} ngày {ngayKham}. " +
                                   $"Vui lòng đến trước 15 phút để làm thủ tục.",
                    LichHenId    = lich.Id,
                    ThoiGianTao  = DateTime.Now
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
