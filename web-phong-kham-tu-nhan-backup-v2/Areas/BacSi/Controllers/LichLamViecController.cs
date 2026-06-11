using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services;

namespace web_phong_kham_tu_nhan.Areas.BacSi.Controllers
{
    [Area("BacSi")]
    [Authorize(Roles = "Bác sĩ")]
    public class LichLamViecController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ThongBaoService _thongBaoSvc;

        public LichLamViecController(ApplicationDbContext context, ThongBaoService thongBaoSvc)
        {
            _context     = context;
            _thongBaoSvc = thongBaoSvc;
        }

        private Models.Entities.BacSi GetCurrentDoctor()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return _context.Doctors.FirstOrDefault(d => d.UserId == userId)!;
        }

        // ── XEM LỊCH TUẦN ──────────────────────────────────────────────────────
        public IActionResult Index(string tuan)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            DateTime startOfWeek;
            if (!DateTime.TryParse(tuan, out startOfWeek))
            {
                int diff = (7 + (int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                startOfWeek = DateTime.Today.AddDays(-diff);
            }
            DateTime endOfWeek = startOfWeek.AddDays(6);

            var lichTuan = _context.LichLamViecBacSis
                .Where(l => l.BacSiId == doctor.Id
                         && l.Ngay >= startOfWeek
                         && l.Ngay <= endOfWeek)
                .OrderBy(l => l.Ngay)
                .ToList();

            var startMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endMonth   = startMonth.AddMonths(1).AddDays(-1);

            ViewBag.Doctor      = doctor;
            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek   = endOfWeek;
            ViewBag.NgayLamThang = _context.LichLamViecBacSis.Count(l =>
                l.BacSiId == doctor.Id && l.Ngay >= startMonth && l.Ngay <= endMonth && l.TrangThai == 0);
            ViewBag.NgayNghiThang = _context.LichLamViecBacSis.Count(l =>
                l.BacSiId == doctor.Id && l.Ngay >= startMonth && l.Ngay <= endMonth
                && (l.TrangThai == 1 || l.TrangThai == 2 || l.TrangThai == 5));
            ViewBag.DonChoDuyet = _context.LichLamViecBacSis.Count(l =>
                l.BacSiId == doctor.Id && (l.TrangThai == 3 || l.TrangThai == 4));

            return View(lichTuan);
        }

        // ── ĐĂNG KÝ CA LÀM (khoảng thời gian hoặc 1 ngày) ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKyCa(string ngayBatDau, string ngayKetThuc,
                                       string ca, int soBenhNhanToiDa, string ghiChu)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            DateTime start, end;
            if (!DateTime.TryParse(ngayBatDau, out start) || !DateTime.TryParse(ngayKetThuc, out end))
            {
                TempData["Error"] = "Ngày không hợp lệ.";
                return RedirectToAction("Index");
            }

            if (start < DateTime.Today)
            {
                TempData["Error"] = "Không thể đăng ký ca làm cho ngày trong quá khứ.";
                return RedirectToAction("Index");
            }

            if (end < start) end = start;

            int soNgayTao = 0;
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                bool existed = _context.LichLamViecBacSis.Any(l =>
                    l.BacSiId == doctor.Id && l.Ngay.Date == d.Date && l.CaLam == ca);
                if (existed) continue;

                var lich = new LichLamViecBacSi
                {
                    BacSiId         = doctor.Id,
                    Ngay            = d,
                    CaLam           = ca,
                    TrangThai       = 4,  // Chờ admin duyệt đăng ký
                    SoBenhNhanToiDa = soBenhNhanToiDa > 0 ? soBenhNhanToiDa : 20,
                    GhiChu          = ghiChu,
                    GhiChuGoc       = ghiChu,
                    ThoiGianTao     = DateTime.Now,
                    TaoLichBoi      = "BacSi"
                };
                _context.LichLamViecBacSis.Add(lich);
                _context.SaveChanges();
                soNgayTao++;

                // Thông báo admin
                var admins = _context.Users.Where(u => u.Role == "Admin").ToList();
                foreach (var admin in admins)
                    _thongBaoSvc.BacSiDangKyLich(admin.Id, doctor.FullName ?? "Bác sĩ",
                                                   d.ToString("dd/MM/yyyy"), lich.Id);
                _thongBaoSvc.SaveChanges();
            }

            TempData[soNgayTao > 0 ? "Success" : "Error"] = soNgayTao > 0
                ? $"Đã gửi {soNgayTao} ca làm cho Admin duyệt."
                : "Tất cả ca đã được đăng ký trước đó.";
            return RedirectToAction("Index", new { tuan = ngayBatDau });
        }

        // ── ĐĂNG KÝ NHANH 1 NGÀY (click ô trống trong lịch) ──────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKyNhanh(string ngay, string ca, int soBenhNhanToiDa, string ghiChu)
        {
            return DangKyCa(ngay, ngay, ca, soBenhNhanToiDa, ghiChu);
        }

        // ── XIN NGHỈ (thường + đột xuất) ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XinNghi(int id, string lyDo, bool dotXuat = false)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            var lich = _context.LichLamViecBacSis
                .FirstOrDefault(l => l.Id == id && l.BacSiId == doctor.Id);
            if (lich == null) return NotFound();

            if (lich.TrangThai != 0)
            {
                TempData["Error"] = "Chỉ có thể xin nghỉ với lịch đang làm việc.";
                return RedirectToAction("Index");
            }

            lich.GhiChuGoc   = lich.GhiChu;
            lich.LyDoXinNghi = lyDo;
            lich.GhiChu      = (dotXuat ? "[Nghỉ đột xuất] " : "[Xin nghỉ] ")
                               + (string.IsNullOrEmpty(lyDo) ? "Không có lý do" : lyDo);
            lich.TrangThai   = 3;
            _context.SaveChanges();

            // Thông báo admin
            var admins = _context.Users.Where(u => u.Role == "Admin").ToList();
            foreach (var admin in admins)
                _thongBaoSvc.BacSiXinNghi(admin.Id, doctor.FullName ?? "Bác sĩ",
                                            lich.Ngay.ToString("dd/MM/yyyy"), lyDo, lich.Id);
            _thongBaoSvc.SaveChanges();

            TempData["Success"] = dotXuat
                ? "Đã gửi đơn nghỉ đột xuất. Chờ Admin xác nhận và thông báo bệnh nhân."
                : "Đã gửi đơn xin nghỉ. Chờ Admin xác nhận.";
            return RedirectToAction("Index");
        }

        // ── HỦY ĐƠN ──────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyDon(int id)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            var lich = _context.LichLamViecBacSis
                .FirstOrDefault(l => l.Id == id && l.BacSiId == doctor.Id);
            if (lich == null) return NotFound();

            if (lich.TrangThai == 4)
            {
                // Hủy đăng ký chờ duyệt → xóa hẳn
                _context.LichLamViecBacSis.Remove(lich);
            }
            else if (lich.TrangThai == 3)
            {
                // Hủy đơn xin nghỉ → trả về làm việc
                lich.GhiChu      = lich.GhiChuGoc;
                lich.LyDoXinNghi = null;
                lich.TrangThai   = 0;
            }
            else
            {
                TempData["Error"] = "Chỉ có thể hủy đơn đang chờ duyệt.";
                return RedirectToAction("Index");
            }

            _context.SaveChanges();
            TempData["Success"] = "Đã hủy đơn thành công.";
            return RedirectToAction("Index");
        }
    }
}
