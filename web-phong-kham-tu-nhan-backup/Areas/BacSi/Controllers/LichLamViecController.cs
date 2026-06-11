using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Areas.BacSi.Controllers
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

        private Models.Entities.BacSi GetCurrentDoctor()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            return _context.Doctors.FirstOrDefault(d => d.UserId == userId);
        }

        // ── XEM LỊCH TUẦN ──
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

            // Thống kê tháng
            var startMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endMonth = startMonth.AddMonths(1).AddDays(-1);

            ViewBag.Doctor = doctor;
            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek;
            ViewBag.NgayLamThang = _context.LichLamViecBacSis.Count(l =>
                l.BacSiId == doctor.Id && l.Ngay >= startMonth && l.Ngay <= endMonth && l.TrangThai == 0);
            ViewBag.NgayNghiThang = _context.LichLamViecBacSis.Count(l =>
                l.BacSiId == doctor.Id && l.Ngay >= startMonth && l.Ngay <= endMonth
                && (l.TrangThai == 1 || l.TrangThai == 2));
            ViewBag.DonChoDuyet = _context.LichLamViecBacSis.Count(l =>
                l.BacSiId == doctor.Id && l.TrangThai == 3);

            return View(lichTuan);
        }

        // ── TỰ ĐĂNG KÝ CA LÀM ──
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

            if (end < start)
            {
                TempData["Error"] = "Ngày kết thúc phải sau ngày bắt đầu.";
                return RedirectToAction("Index");
            }

            int soNgayTao = 0;
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                bool existed = _context.LichLamViecBacSis.Any(l =>
                    l.BacSiId == doctor.Id && l.Ngay.Date == d.Date && l.CaLam == ca);
                if (existed) continue;

                _context.LichLamViecBacSis.Add(new LichLamViecBacSi
                {
                    BacSiId = doctor.Id,
                    Ngay = d,
                    CaLam = ca,
                    TrangThai = 0,
                    SoBenhNhanToiDa = soBenhNhanToiDa > 0 ? soBenhNhanToiDa : 20,
                    GhiChu = ghiChu,
                    GhiChuGoc = ghiChu,
                    ThoiGianTao = DateTime.Now,
                    TaoLichBoi = "BacSi"
                });
                soNgayTao++;
            }

            _context.SaveChanges();
            TempData["Success"] = "Đã đăng ký " + soNgayTao + " ca làm việc. Chờ admin xác nhận.";
            return RedirectToAction("Index", new { tuan = ngayBatDau });
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

            // Backup ghi chú gốc trước khi ghi đè
            lich.GhiChuGoc = lich.GhiChu;
            lich.LyDoXinNghi = lyDo;
            lich.GhiChu = "[Xin nghỉ] " + (string.IsNullOrEmpty(lyDo) ? "Không có lý do" : lyDo);
            lich.TrangThai = 3;

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

            if (lich.TrangThai != 3)
            {
                TempData["Error"] = "Chỉ có thể hủy đơn đang chờ duyệt.";
                return RedirectToAction("Index");
            }

            // Restore lại ghi chú gốc
            lich.GhiChu = lich.GhiChuGoc;
            lich.LyDoXinNghi = null;
            lich.TrangThai = 0;

            _context.SaveChanges();
            TempData["Success"] = "Đã hủy đơn xin nghỉ. Lịch trở về trạng thái làm việc.";
            return RedirectToAction("Index");
        }
    }
}