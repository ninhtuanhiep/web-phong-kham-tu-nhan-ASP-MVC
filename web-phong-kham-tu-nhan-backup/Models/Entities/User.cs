namespace web_phong_kham_tu_nhan.Models.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}
