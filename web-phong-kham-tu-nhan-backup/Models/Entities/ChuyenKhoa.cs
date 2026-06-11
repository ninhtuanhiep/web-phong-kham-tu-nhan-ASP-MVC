using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace web_phong_kham_tu_nhan.Models.Entities
{
    public class ChuyenKhoa
    {
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public string? MoTa { get; set; }

        public ICollection<BacSi>? BacSis {  get; set; }
        public ICollection<BenhNhan>? BenhNhans { get; set; }
        public ICollection<LichHen>? Appointments { get; set; }
    }
}