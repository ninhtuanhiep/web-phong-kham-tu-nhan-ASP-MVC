using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LichLamViecBacSiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichLamViecBacSiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── TRANG CHÍNH: BẢNG TUẦN / THÁNG ──
        public IActionResult Index(int? bacSiId, string view, string tuan)
        {
            // Mặc định xem theo tuần hiện tại
            DateTime startOfWeek;
            if (!DateTime.TryParse(tuan, out startOfWeek))
            {
                int diff = (7 + (int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                startOfWeek = DateTime.Today.AddDays(-diff);
            }

            DateTime endOfWeek = startOfWeek.AddDays(6);

            var bacSis = _context.Doctors
                .Where(d => d.TrangThai != 3)
                .OrderBy(d => d.FullName)
                .ToList();

            ViewBag.BacSis = new SelectList(bacSis, "Id", "FullName", bacSiId);
            ViewBag.BacSiId = bacSiId;
            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek;
            ViewBag.ViewMode = view ?? "tuan";

            IQueryable<LichLamViecBacSi> query = _context.LichLamViecBacSis
                .Include(l => l.BacSi);

            if (bacSiId.HasValue)
                query = query.Where(l => l.BacSiId == bacSiId.Value);

            if (ViewBag.ViewMode == "thang")
            {
                var startOfMonth = new DateTime(startOfWeek.Year, startOfWeek.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                ViewBag.StartOfMonth = startOfMonth;
                ViewBag.EndOfMonth = endOfMonth;
                var lichThang = query
                    .Where(l => l.Ngay >= startOfMonth && l.Ngay <= endOfMonth)
                    .OrderBy(l => l.Ngay).ThenBy(l => l.BacSiId)
                    .ToList();
                return View(lichThang);
            }

            var lichTuan = query
                .Where(l => l.Ngay >= startOfWeek && l.Ngay <= endOfWeek)
                .OrderBy(l => l.Ngay).ThenBy(l => l.BacSiId)
                .ToList();

            return View(lichTuan);
        }

        // ── TẠO LỊCH ──
        [HttpGet]
        public IActionResult Create(string ngay)
        {
            DateTime selectedDate;
            if (!DateTime.TryParse(ngay, out selectedDate))
                selectedDate = DateTime.Today;

            ViewBag.BacSis = new SelectList(
                _context.Doctors.Where(d => d.TrangThai != 3).OrderBy(d => d.FullName),
                "Id", "FullName");
            ViewBag.SelectedDate = selectedDate;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int bacSiId, string ngayBatDau, string ngayKetThuc,
                                    string ca, int soBenhNhanToiDa, string ghiChu)
        {
            DateTime start, end;
            if (!DateTime.TryParse(ngayBatDau, out start) || !DateTime.TryParse(ngayKetThuc, out end))
            {
                TempData["Error"] = "Ngày không hợp lệ.";
                return RedirectToAction("Create");
            }

            if (end < start)
            {
                TempData["Error"] = "Ngày kết thúc phải sau ngày bắt đầu.";
                return RedirectToAction("Create");
            }

            int soNgayTao = 0;
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                // Bỏ qua nếu đã có lịch trong ngày + ca đó
                bool existed = _context.LichLamViecBacSis.Any(l =>
                    l.BacSiId == bacSiId &&
                    l.Ngay.Date == d.Date &&
                    l.CaLam == ca);
                if (existed) continue;

                _context.LichLamViecBacSis.Add(new LichLamViecBacSi
                {
                    BacSiId = bacSiId,
                    Ngay = d,
                    CaLam = ca,
                    TrangThai = 0,
                    SoBenhNhanToiDa = soBenhNhanToiDa > 0 ? soBenhNhanToiDa : 20,
                    GhiChu = ghiChu,
                    ThoiGianTao = DateTime.Now,
                    TaoLichBoi = "Admin"
                });
                soNgayTao++;
            }

            _context.SaveChanges();
            TempData["Success"] = "Đã tạo " + soNgayTao + " lịch làm việc thành công.";
            return RedirectToAction("Index");
        }

        // ── DUYỆT / TỪ CHỐI ĐƠN XIN NGHỈ ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DuyetNghi(int id, bool chaphuan)
        {
            var lich = _context.LichLamViecBacSis.Find(id);
            if (lich == null) return NotFound();

            lich.TrangThai = chaphuan ? 1 : 0; // 1=Nghỉ phép, 0=Quay lại làm việc
            _context.SaveChanges();

            TempData["Success"] = chaphuan
                ? "Đã duyệt đơn xin nghỉ."
                : "Đã từ chối đơn xin nghỉ, lịch giữ nguyên.";
            return RedirectToAction("Index");
        }

        // ── XÓA LỊCH ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var lich = _context.LichLamViecBacSis.Find(id);
            if (lich == null) return NotFound();
            _context.LichLamViecBacSis.Remove(lich);
            _context.SaveChanges();
            TempData["Success"] = "Đã xóa lịch làm việc.";
            return RedirectToAction("Index");
        }

        // ── AJAX: LẤY LỊCH THEO TUẦN (cho calendar) ──
        public IActionResult GetLich(string startDate, string endDate, int? bacSiId)
        {
            DateTime start, end;
            if (!DateTime.TryParse(startDate, out start) || !DateTime.TryParse(endDate, out end))
                return Json(new List<object>());

            var query = _context.LichLamViecBacSis
                .Include(l => l.BacSi)
                .Where(l => l.Ngay >= start && l.Ngay <= end);

            if (bacSiId.HasValue)
                query = query.Where(l => l.BacSiId == bacSiId.Value);

            var result = query.Select(l => new {
                id = l.Id,
                bacSiId = l.BacSiId,
                tenBacSi = l.BacSi.FullName,
                ngay = l.Ngay.ToString("yyyy-MM-dd"),
                ca = l.CaLam,
                trangThai = l.TrangThai,
                soToiDa = l.SoBenhNhanToiDa,
                ghiChu = l.GhiChu
            }).ToList();

            return Json(result);
        }

        // ── DANH SÁCH ĐƠN XIN NGHỈ CHỜ DUYỆT ──
        public IActionResult DonXinNghi()
        {
            var donChoduyet = _context.LichLamViecBacSis
                .Include(l => l.BacSi)
                .Where(l => l.TrangThai == 3)
                .OrderBy(l => l.Ngay)
                .ToList();
            return View(donChoduyet);
        }
    }
}
