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

            var lichTuan = query
                .Where(l => l.Ngay >= startOfWeek && l.Ngay <= endOfWeek)
                .OrderBy(l => l.Ngay).ThenBy(l => l.BacSiId)
                .ToList();
            ViewBag.DonChoDuyet = _context.LichLamViecBacSis.Count(l => l.TrangThai == 3);

            return View(lichTuan);
        }

        // ── TẠO LỊCH ──
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
                    GhiChuGoc = ghiChu,
                    ThoiGianTao = DateTime.Now,
                    TaoLichBoi = "Admin"
                });
                soNgayTao++;
            }
            _context.SaveChanges();
            TempData["Success"] = "Đã tạo " + soNgayTao + " lịch làm việc.";
            return RedirectToAction("Index", new { tuan = ngayBatDau });
        }

        // ── DANH SÁCH ĐƠN XIN NGHỈ ──
        public IActionResult DonXinNghi()
        {
            var dons = _context.LichLamViecBacSis
                .Include(l => l.BacSi)
                .Where(l => l.TrangThai == 3)
                .OrderBy(l => l.Ngay)
                .ToList();
            return View(dons);
        }

        // ── CHI TIẾT ĐƠN XIN NGHỈ ──
        public IActionResult ChiTietDon(int id)
        {
            var lich = _context.LichLamViecBacSis
                .Include(l => l.BacSi)
                .FirstOrDefault(l => l.Id == id);
            if (lich == null) return NotFound();
            return View(lich);
        }

        // ── DUYỆT / TỪ CHỐI ĐƠN XIN NGHỈ ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DuyetNghi(int id, bool chaphuan)
        {
            var lich = _context.LichLamViecBacSis.Find(id);
            if (lich == null) return NotFound();

            if (chaphuan)
            {
                lich.TrangThai = 1; // Duyệt nghỉ
            }
            else
            {
                lich.TrangThai = 0; // Từ chối
                lich.GhiChu = lich.GhiChuGoc; // Restore ghi chú gốc
                lich.LyDoXinNghi = null;
            }
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
    }
}