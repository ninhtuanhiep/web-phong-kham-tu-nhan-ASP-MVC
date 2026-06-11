using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_phong_kham_tu_nhan.Models.Entities
{
    /// <summary>
    /// Bảng thông báo hệ thống.
    /// LoaiThongBao:
    ///   "NhacLich"       – nhắc lịch hẹn sắp đến (gửi cho bệnh nhân)
    ///   "BacSiNghi"      – bác sĩ xin nghỉ đột xuất (gửi cho bệnh nhân bị ảnh hưởng)
    ///   "LichDuyet"      – admin duyệt / từ chối lịch đăng ký (gửi cho bác sĩ)
    ///   "DangKyLich"     – bác sĩ đăng ký lịch mới chờ duyệt (gửi cho admin)
    ///   "XinNghi"        – bác sĩ gửi đơn xin nghỉ (gửi cho admin)
    /// NguoiNhan:
    ///   UserId của người nhận thông báo
    /// </summary>
    public class ThongBao
    {
        public int Id { get; set; }

        [Required]
        public int NguoiNhanId { get; set; }          // UserId người nhận

        [Required]
        public string LoaiThongBao { get; set; } = ""; // Xem comment trên

        [Required]
        public string TieuDe { get; set; } = "";

        [Required]
        public string NoiDung { get; set; } = "";

        public bool DaDoc { get; set; } = false;

        public DateTime ThoiGianTao { get; set; } = DateTime.Now;

        // Liên kết tùy chọn
        public int? LichHenId { get; set; }
        public int? LichLamViecId { get; set; }

        [ForeignKey("NguoiNhanId")]
        public User? NguoiNhan { get; set; }

        [ForeignKey("LichHenId")]
        public LichHen? LichHen { get; set; }
    }
}
