using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Areas.BacSis.Controllers
{
    [Area("BacSi")]
    [Authorize(Roles = "Bác sĩ")]
    public class LichLamViecController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichLamViecController(ApplicationDbContext context)
        {
            _context = context;
        }

        private BacSi GetCurrentDoctor()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            return _context.Doctors.FirstOrDefault(d => d.UserId == userId);
        }

        // ── XEM LỊCH LÀM VIỆC ──
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

            ViewBag.Doctor = doctor;
            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek;

            // Thống kê tháng này
            var startMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endMonth = startMonth.AddMonths(1).AddDays(-1);
            ViewBag.NgayLamThang = _context.LichLamViecBacSis.Count(l =>
                l.BacSiId == doctor.Id &&
                l.Ngay >= startMonth &&
                l.Ngay <= endMonth &&
                l.TrangThai == 0);
            ViewBag.NgayNghiThang = _context.LichLamViecBacSis.Count(l =>
                l.BacSiId == doctor.Id &&
                l.Ngay >= startMonth &&
                l.Ngay <= endMonth &&
                (l.TrangThai == 1 || l.TrangThai == 2));
            ViewBag.DonChoDuyet = _context.LichLamViecBacSis.Count(l =>
                l.BacSiId == doctor.Id && l.TrangThai == 3);

            return View(lichTuan);
        }

        // ── XIN NGHỈ ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XinNghi(int id, string lyDo)
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

            lich.TrangThai = 3; // Chờ admin duyệt
            lich.GhiChu = string.IsNullOrEmpty(lyDo)
                                 ? lich.GhiChu
                                 : "[Xin nghỉ] " + lyDo;
            _context.SaveChanges();

            TempData["Success"] = "Đã gửi đơn xin nghỉ. Chờ admin xác nhận.";
            return RedirectToAction("Index");
        }

        // ── HỦY ĐƠN XIN NGHỈ ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyDon(int id)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            var lich = _context.LichLamViecBacSis
                .FirstOrDefault(l => l.Id == id && l.BacSiId == doctor.Id);

            if (lich == null) return NotFound();

            if (lich.TrangThai == 3) // Đang chờ duyệt -> hủy về làm việc
            {
                lich.TrangThai = 0;
                if (lich.GhiChu != null && lich.GhiChu.StartsWith("[Xin nghỉ] "))
                    lich.GhiChu = lich.GhiChu.Substring(11);
                _context.SaveChanges();
                TempData["Success"] = "Đã hủy đơn xin nghỉ.";
            }

            return RedirectToAction("Index");
        }
    }
}
