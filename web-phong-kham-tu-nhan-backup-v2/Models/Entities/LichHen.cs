using System.ComponentModel.DataAnnotations;

using System.Numerics;

namespace web_phong_kham_tu_nhan.Models.Entities
{
    public class LichHen
    {
        public int Id { get; set; }
        [Required]
        public int BenhNhanId { get; set; }
        [Required]
        public int BacSiId { get; set; }
        [Required]
        public int ChuyenKhoaId { get; set; }
        [Required]
        public DateTime AppointmentDate { get; set; }
        //[Required]
        public string? TimeSlot { get; set; }   

        public string? LyDoKham { get; set; }

        public int TrangThai { get; set; }
        public DateTime? RescheduleDate { get; set; }
        public string? RescheduleTimeSlot { get; set; }
        public string? RescheduleNote { get; set; }   // Lý do đổi lịch
        public DateTime? RescheduleAt { get; set; }   // Thời điểm gửi yêu cầu đổi
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public BacSi? BacSi { get; set; }

        public BenhNhan? BenhNhan { get; set; }

        public ChuyenKhoa? ChuyenKhoa { get; set; }
    }
}
