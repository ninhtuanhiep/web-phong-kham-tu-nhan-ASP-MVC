using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace web_phong_kham_tu_nhan.Models.Entities
{
    public class LichLamViecBacSi
    {
        public int Id { get; set; }

        [Required]
        public int BacSiId { get; set; }

        [Required]
        public DateTime Ngay { get; set; }

        // Ca làm việc: "Sang" | "Chieu" | "CaNgay"
        [Required]
        public string? CaLam { get; set; }

        // Trạng thái: 0=Làm việc | 1=Nghỉ phép | 2=Nghỉ bệnh | 3=Chờ duyệt (bác sĩ xin nghỉ)
        public int TrangThai { get; set; } = 0;

        public int SoBenhNhanToiDa { get; set; } = 20;

        public string? GhiChu { get; set; }

        public DateTime ThoiGianTao { get; set; } = DateTime.Now;

        public string? TaoLichBoi { get; set; } // Dùng để phân biệt xem "Admin" hoặc "BacSi" là người tạo lịch

        [ForeignKey("BacSiId")]
        public BacSi? BacSi { get; set; }
    }
}
