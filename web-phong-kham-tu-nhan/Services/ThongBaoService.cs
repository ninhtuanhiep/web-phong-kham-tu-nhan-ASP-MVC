using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Services
{
    /// <summary>
    /// Dịch vụ tập trung để tạo thông báo trong hệ thống.
    /// </summary>
    public class ThongBaoService
    {
        private readonly ApplicationDbContext _context;

        public ThongBaoService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── GỬI THÔNG BÁO CHUNG ──────────────────────────────────────
        public void GuiThongBao(int nguoiNhanId, string loai, string tieuDe, string noiDung,
                                 int? lichHenId = null, int? lichLamViecId = null)
        {
            _context.ThongBaos.Add(new ThongBao
            {
                NguoiNhanId   = nguoiNhanId,
                LoaiThongBao  = loai,
                TieuDe        = tieuDe,
                NoiDung       = noiDung,
                LichHenId     = lichHenId,
                LichLamViecId = lichLamViecId,
                ThoiGianTao   = DateTime.Now,
                DaDoc         = false
            });
        }

        // ── NHẮC LỊCH HẸN CHO BỆNH NHÂN ─────────────────────────────
        public void NhacLichHen(LichHen lichHen, string tenBacSi, string tenBenhNhan,
                                 int nguoiNhanUserId)
        {
            string ngay = lichHen.AppointmentDate.ToString("dd/MM/yyyy");
            string gio  = lichHen.TimeSlot ?? "–";
            GuiThongBao(
                nguoiNhanUserId,
                "NhacLich",
                $"⏰ Nhắc lịch khám ngày {ngay}",
                $"Bạn có lịch khám với Bác sĩ {tenBacSi} vào lúc {gio} ngày {ngay}. Vui lòng đến đúng giờ.",
                lichHenId: lichHen.Id
            );
        }

        // ── THÔNG BÁO BÁC SĨ NGHỈ ĐỘT XUẤT ──────────────────────────
        public void BacSiNghiDotXuat(LichHen lichHen, string tenBacSi, int nguoiNhanUserId)
        {
            string ngay = lichHen.AppointmentDate.ToString("dd/MM/yyyy");
            GuiThongBao(
                nguoiNhanUserId,
                "BacSiNghi",
                $"⚠️ Lịch khám ngày {ngay} bị thay đổi",
                $"Bác sĩ {tenBacSi} có việc đột xuất và không thể khám vào ngày {ngay}. " +
                $"Lịch hẹn của bạn đã được chuyển sang trạng thái chờ sắp xếp lại. " +
                $"Phòng khám sẽ liên hệ để đặt lại lịch sớm nhất.",
                lichHenId: lichHen.Id
            );
        }

        // ── THÔNG BÁO CHO ADMIN KHI BÁC SĨ ĐĂNG KÝ LỊCH ────────────
        public void BacSiDangKyLich(int adminUserId, string tenBacSi, string ngayDangKy,
                                     int lichLamViecId)
        {
            GuiThongBao(
                adminUserId,
                "DangKyLich",
                $"📋 Bác sĩ {tenBacSi} đăng ký lịch làm việc",
                $"Bác sĩ {tenBacSi} vừa đăng ký ca làm việc vào ngày {ngayDangKy}. " +
                $"Vui lòng vào trang Quản lý lịch làm việc để xem xét và duyệt.",
                lichLamViecId: lichLamViecId
            );
        }

        // ── THÔNG BÁO CHO ADMIN KHI BÁC SĨ XIN NGHỈ ─────────────────
        public void BacSiXinNghi(int adminUserId, string tenBacSi, string ngayNghi,
                                   string lyDo, int lichLamViecId)
        {
            GuiThongBao(
                adminUserId,
                "XinNghi",
                $"🏥 Bác sĩ {tenBacSi} xin nghỉ ngày {ngayNghi}",
                $"Bác sĩ {tenBacSi} gửi đơn xin nghỉ ngày {ngayNghi}. " +
                $"Lý do: {(string.IsNullOrEmpty(lyDo) ? "Không có lý do" : lyDo)}. " +
                $"Vui lòng vào trang Duyệt đơn xin nghỉ để xem xét.",
                lichLamViecId: lichLamViecId
            );
        }

        // ── THÔNG BÁO CHO BÁC SĨ KHI ADMIN DUYỆT LỊCH ───────────────
        public void AdminDuyetLich(int bacSiUserId, string ngay, bool chapNhan,
                                    string? ghiChuAdmin, int lichLamViecId)
        {
            if (chapNhan)
            {
                GuiThongBao(
                    bacSiUserId,
                    "LichDuyet",
                    $"✅ Lịch làm việc ngày {ngay} đã được duyệt",
                    $"Ca làm việc ngày {ngay} của bạn đã được Admin phê duyệt." +
                    (string.IsNullOrEmpty(ghiChuAdmin) ? "" : $" Ghi chú: {ghiChuAdmin}"),
                    lichLamViecId: lichLamViecId
                );
            }
            else
            {
                GuiThongBao(
                    bacSiUserId,
                    "LichDuyet",
                    $"❌ Lịch làm việc ngày {ngay} bị từ chối",
                    $"Ca làm việc ngày {ngay} của bạn đã bị Admin từ chối." +
                    (string.IsNullOrEmpty(ghiChuAdmin) ? "" : $" Lý do: {ghiChuAdmin}"),
                    lichLamViecId: lichLamViecId
                );
            }
        }

        // ── THÔNG BÁO CHO BÁC SĨ KHI ADMIN DUYỆT ĐƠN XIN NGHỈ ──────
        public void AdminDuyetDonNghi(int bacSiUserId, string ngay, bool chapNhan,
                                       int lichLamViecId)
        {
            if (chapNhan)
            {
                GuiThongBao(
                    bacSiUserId,
                    "LichDuyet",
                    $"✅ Đơn xin nghỉ ngày {ngay} đã được chấp thuận",
                    $"Admin đã duyệt đơn xin nghỉ của bạn vào ngày {ngay}. " +
                    $"Các lịch hẹn trong ngày đó đã được cập nhật.",
                    lichLamViecId: lichLamViecId
                );
            }
            else
            {
                GuiThongBao(
                    bacSiUserId,
                    "LichDuyet",
                    $"❌ Đơn xin nghỉ ngày {ngay} bị từ chối",
                    $"Admin đã từ chối đơn xin nghỉ của bạn vào ngày {ngay}. " +
                    $"Lịch làm việc vẫn giữ nguyên.",
                    lichLamViecId: lichLamViecId
                );
            }
        }

        public void SaveChanges() => _context.SaveChanges();
    }
}
