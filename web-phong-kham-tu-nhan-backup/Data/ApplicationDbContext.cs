using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
        {
        }

        public DbSet<BacSi> Doctors { get; set; }
        public DbSet<ChuyenKhoa> Specialties { get; set; }
        public DbSet<BenhNhan> Patients { get; set; }
        public DbSet<LichHen> Appointments { get; set; }
        public DbSet<LichLamViecBacSi> LichLamViecBacSis { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<HoSoBacSi> HoSoBacSis { get; set; }
        public DbSet<YeuCauCapNhat> YeuCauCapNhats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LichHen>()
                .HasOne(a => a.ChuyenKhoa)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ChuyenKhoaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LichHen>()
                .HasOne(a => a.BacSi)
                .WithMany(d => d.LichHens)
                .HasForeignKey(a => a.BacSiId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LichHen>()
                .HasOne(a => a.BenhNhan)
                .WithMany(p => p.LichHens)
                .HasForeignKey(a => a.BenhNhanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoSoBacSi>()
                .HasOne(h => h.BacSi)
                .WithOne(b => b.HoSoBacSi)
                .HasForeignKey<HoSoBacSi>(h => h.BacSiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<YeuCauCapNhat>()
                .HasOne(y => y.BacSi)
                .WithMany()
                .HasForeignKey(y => y.BacSiId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
