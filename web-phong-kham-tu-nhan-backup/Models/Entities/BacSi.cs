using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_phong_kham_tu_nhan.Models.Entities

{
    public class BacSi
    {
        [Key]
        public int Id { get; set; }
        public int? UserId { get; set; }

        [Required]
        public string? FullName { get; set; }
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public string? ImageUrl { get; set; } // lưu đường dẫn
        [NotMapped]
        public IFormFile? ImageFile { get; set; } // dùng để upload
        public string? tieuSu { get; set; }
        public string? diaChi { get; set; }
        public string? gioiTinh { get; set; }
        public DateTime? ngaySinh { get; set; }
        public int TrangThai { get; set; }
        public int ChuyenKhoaId { get; set; }
        [ForeignKey("ChuyenKhoaId")]
        public ChuyenKhoa? ChuyenKhoa { get; set; }
        public List<LichHen>? LichHens { get; set; }
        public HoSoBacSi? HoSoBacSi { get; set; }
    }
}
