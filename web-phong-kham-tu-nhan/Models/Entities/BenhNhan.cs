using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_phong_kham_tu_nhan.Models.Entities
{
    public class BenhNhan
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage ="Tên bệnh nhân không được để trống!")]
        public string? FullName { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? GioiTinh { get; set; }
        public string? PhoneNumber { get; set; }
        public string? DiaChi { get; set; }
        public string? Email { get; set; }
        public string? LichSuYTe { get; set; }
        public int? TrangThai { get; set; }
        public List<LichHen>? LichHens { get; set; }
        public int? ChuyenKhoaId { get; set; }
        public ChuyenKhoa? ChuyenKhoa { get; set; }
    }
}
