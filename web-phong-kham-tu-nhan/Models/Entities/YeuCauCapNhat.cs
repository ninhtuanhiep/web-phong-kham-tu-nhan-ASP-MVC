using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_phong_kham_tu_nhan.Models.Entities
{
    public class YeuCauCapNhat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BacSiId { get; set; }

        // Field muốn cập nhật: "HocVi" | "HocHam" | "SoCCHN" | "KinhNghiem" | ...
        [Required]
        // Tên trường 
        public string? TenTruong { get; set; }

        // Giá trị hiện tại (để admin so sánh)
        public string? GiaTriCu { get; set; }

        // Giá trị bác sĩ muốn đổi thành
        [Required]
        public string? GiaTriMoi { get; set; }

        // Lý do / giải thích của bác sĩ
        public string? LyDo { get; set; }
            
        // TrangThai: 0=Chờ duyệt | 1=Đã duyệt | 2=Từ chối
        public int TrangThai { get; set; } = 0;

        // Lý do từ chối của admin
        public string? LyDoTuChoi { get; set; }

        public DateTime TaoLuc { get; set; } = DateTime.Now;
        public DateTime? DuyetLuc { get; set; }
        public string? DuyetBoi { get; set; } // Username admin duyệt

        [ForeignKey("BacSiId")]
        public BacSi? BacSi { get; set; }
    }
}
