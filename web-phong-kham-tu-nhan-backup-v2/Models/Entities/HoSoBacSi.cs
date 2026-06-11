using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_phong_kham_tu_nhan.Models.Entities
{
    public class HoSoBacSi
    {
        [Key]
        public int Id { get; set; }

        // 1-1 với BacSi
        [Required]
        public int BacSiId { get; set; }

        // ── HỌC VỊ & BẰNG CẤP ──
        // Ví dụ: "Bác sĩ đa khoa", "Thạc sĩ", "Tiến sĩ", "CKI", "CKII", "Nội trú"
        public string? HocVi { get; set; }

        // ── HỌC HÀM ──
        // Ví dụ: "Giáo sư", "Phó Giáo sư" (để trống nếu không có)
        public string? HocHam { get; set; }

        // ── CHỨNG CHỈ HÀNH NGHỀ ──
        // Số CCHN, phạm vi hành nghề (Nội, Ngoại, Sản, Nhi,...)
        public string? SoCCHN { get; set; }
        public string? PhamViHanhNghe { get; set; }
        public DateTime? NgayCapCCHN { get; set; }
        public DateTime? NgayHetHanCCHN { get; set; }

        // ── KINH NGHIỆM LÀM VIỆC ──
        // Lưu dạng text mô tả (các bệnh viện, phòng khám, vị trí từng đảm nhiệm)
        public string? KinhNghiem { get; set; }

        // ── THÔNG TIN BỔ SUNG ──
        public int NamKinhNghiem { get; set; }
        public string? TruongDaoTao { get; set; }   // Nơi tốt nghiệp
        public string? ChuyenMonSau { get; set; }   // Chuyên môn sâu / thế mạnh
        public string? NgonNgu { get; set; }        // Ngôn ngữ khám (VD: "Việt, Anh")
        public string? GiaiThuong { get; set; }     // Giải thưởng, danh hiệu nếu có

        public DateTime CapNhatLuc { get; set; } = DateTime.Now;

        [ForeignKey("BacSiId")]
        public BacSi? BacSi { get; set; }
    }
}
