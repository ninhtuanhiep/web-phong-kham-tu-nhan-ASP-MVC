using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Areas.BacSis.Controllers
{
    [Area("BacSi")]
    [Authorize] 
    public class LichHenController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichHenController(ApplicationDbContext context)
        {
            _context = context;
        }

        private BacSi GetCurrentDoctor()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            return _context.Doctors.FirstOrDefault(d => d.UserId == userId);
        }

        // ── TRANG LỊCH HẸN HÔM NAY ──
        public IActionResult Index(string ngay)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            DateTime selectedDate;
            if (!DateTime.TryParse(ngay, out selectedDate))
                selectedDate = DateTime.Today;

            var lichHens = _context.Appointments
                .Where(a => a.BacSiId == doctor.Id
                         && a.AppointmentDate.Date == selectedDate.Date)
                .Include(a => a.BenhNhan)
                .Include(a => a.ChuyenKhoa)
                .OrderBy(a => a.TimeSlot)
                .ToList();

            ViewBag.SelectedDate = selectedDate;
            ViewBag.Doctor = doctor;

            // Thống kê nhanh
            ViewBag.TongHomNay = lichHens.Count();
            ViewBag.ChoXacNhan = lichHens.Count(a => a.TrangThai == 0);
            ViewBag.DaXacNhan = lichHens.Count(a => a.TrangThai == 1);
            ViewBag.HoanThanh = lichHens.Count(a => a.TrangThai == 2);
            ViewBag.DaHuy = lichHens.Count(a => a.TrangThai == 3);

            return View(lichHens);
        }

        // ── XÁC NHẬN LỊCH HẸN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhan(int id, string ngay)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            var lich = _context.Appointments
                .FirstOrDefault(a => a.Id == id && a.BacSiId == doctor.Id);

            if (lich == null) return NotFound();

            lich.TrangThai = 1; // Đã xác nhận
            _context.SaveChanges();

            TempData["Success"] = "Đã xác nhận lịch hẹn #LH-" + id.ToString("D4");
            return RedirectToAction("Index", new { ngay });
        }

        // ── HOÀN THÀNH LỊCH HẸN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HoanThanh(int id, string ngay)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            var lich = _context.Appointments
                .FirstOrDefault(a => a.Id == id && a.BacSiId == doctor.Id);

            if (lich == null) return NotFound();

            lich.TrangThai = 2; // Hoàn thành
            _context.SaveChanges();

            TempData["Success"] = "Đã hoàn thành lịch hẹn #LH-" + id.ToString("D4");
            return RedirectToAction("Index", new { ngay });
        }

        // ── HỦY LỊCH HẸN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyLich(int id, string ngay, string lyDo)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            var lich = _context.Appointments
                .FirstOrDefault(a => a.Id == id && a.BacSiId == doctor.Id);

            if (lich == null) return NotFound();

            lich.TrangThai = 3; // Đã hủy
            _context.SaveChanges();

            TempData["Error"] = "Đã hủy lịch hẹn #LH-" + id.ToString("D4");
            return RedirectToAction("Index", new { ngay });
        }

        // ── CHI TIẾT BỆNH NHÂN (AJAX) ──
        public IActionResult ChiTietBenhNhan(int lichHenId)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            var lich = _context.Appointments
                .Include(a => a.BenhNhan)
                .Include(a => a.ChuyenKhoa)
                .FirstOrDefault(a => a.Id == lichHenId && a.BacSiId == doctor.Id);

            if (lich == null) return NotFound();

            return Json(new
            {
                id = lich.Id,
                hoTen = lich.BenhNhan != null ? lich.BenhNhan.FullName : "Không rõ",
                email = lich.BenhNhan != null ? lich.BenhNhan.Email : "",
                phone = lich.BenhNhan != null ? lich.BenhNhan.PhoneNumber : "",
                ngaySinh = lich.BenhNhan != null && lich.BenhNhan.NgaySinh.HasValue
                                   ? lich.BenhNhan.NgaySinh.Value.ToString("dd/MM/yyyy") : "Chưa cập nhật",
                gioiTinh = lich.BenhNhan != null ? lich.BenhNhan.GioiTinh : "",
                diaChi = lich.BenhNhan != null ? lich.BenhNhan.DiaChi : "",
                lichSuYTe = lich.BenhNhan != null ? lich.BenhNhan.LichSuYTe : "",
                lyDoKham = lich.LyDoKham,
                timeSlot = lich.TimeSlot,
                chuyenKhoa = lich.ChuyenKhoa != null ? lich.ChuyenKhoa.Name : "",
                trangThai = lich.TrangThai
            });
        }
    }
}