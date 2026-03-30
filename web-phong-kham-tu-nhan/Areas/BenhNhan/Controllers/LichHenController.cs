using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Areas.BenhNhan.Controllers
{
    [Area("BenhNhan")]
    [Authorize]
    public class LichHenController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichHenController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Models.Entities.BenhNhan GetCurrentPatient()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            return _context.Patients.FirstOrDefault(p => p.UserId == userId);
        }

        // ── Helper: kiểm tra bác sĩ có lịch làm việc trong ngày + ca không ──
        private bool BacSiCoLichLam(int bacSiId, DateTime ngay, string timeSlot)
        {
            // Xác định ca dựa vào time slot
            // Sáng: 07:30 - 11:30 | Chiều: 13:00 - 17:00
            string ca = XacDinhCa(timeSlot);

            return _context.LichLamViecBacSis.Any(l =>
                l.BacSiId == bacSiId &&
                l.Ngay.Date == ngay.Date &&
                l.TrangThai == 0 && // Đang làm việc (không phải nghỉ)
                (l.CaLam == ca || l.CaLam == "CaNgay"));
        }

        private string XacDinhCa(string timeSlot)
        {
            if (string.IsNullOrEmpty(timeSlot)) return "Sang";
            // Lấy giờ bắt đầu từ slot "07:30-08:00"
            string[] parts = timeSlot.Split('-');
            if (parts.Length == 0) return "Sang";
            string gioStr = parts[0].Trim().Split(':')[0];
            int gio;
            if (!int.TryParse(gioStr, out gio)) return "Sang";
            return gio < 12 ? "Sang" : "Chieu";
        }

        // ── TRANG ĐẶT LỊCH ──
        public IActionResult Create()
        {
            ViewBag.ChuyenKhoas = _context.Specialties.ToList();
            return View();
        }

        // ── AJAX: Lấy bác sĩ theo chuyên khoa (chỉ bác sĩ đang làm việc) ──
        public IActionResult GetDoctorsBySpecialty(int specialtyId)
        {
            var doctors = _context.Doctors
                .Where(d => d.ChuyenKhoaId == specialtyId && d.TrangThai == 1)
                .Select(d => new {
                    id = d.Id,
                    fullName = d.FullName,
                    imageUrl = d.ImageUrl != null ? d.ImageUrl : "/Images/default-avatar.png",
                    tieuSu = d.tieuSu != null ? d.tieuSu : "Bác sĩ chuyên khoa"
                })
                .ToList();
            return Json(doctors);
        }

        // ── AJAX: Lấy slot đã đặt + slot không có lịch làm việc ──
        public IActionResult GetBookedSlots(int doctorId, string date)
        {
            DateTime parsedDate;
            if (!DateTime.TryParse(date, out parsedDate))
                return Json(new { booked = new List<string>(), noSchedule = new List<string>() });

            // Slot đã có người đặt
            var booked = _context.Appointments
                .Where(a => a.BacSiId == doctorId
                         && a.AppointmentDate.Date == parsedDate.Date
                         && a.TrangThai != 3)
                .Select(a => a.TimeSlot)
                .ToList();

            // Kiểm tra bác sĩ có lịch làm việc trong ngày không
            bool coLichSang = _context.LichLamViecBacSis.Any(l =>
                l.BacSiId == doctorId && l.Ngay.Date == parsedDate.Date
                && l.TrangThai == 0 && (l.CaLam == "Sang" || l.CaLam == "CaNgay"));
            bool coLichChieu = _context.LichLamViecBacSis.Any(l =>
                l.BacSiId == doctorId && l.Ngay.Date == parsedDate.Date
                && l.TrangThai == 0 && (l.CaLam == "Chieu" || l.CaLam == "CaNgay"));

            // Danh sách tất cả slots
            string[] allSlots = {
                "07:30-08:00","08:00-08:30","08:30-09:00","09:00-09:30",
                "09:30-10:00","10:00-10:30","10:30-11:00","11:00-11:30",
                "13:00-13:30","13:30-14:00","14:00-14:30","14:30-15:00",
                "15:00-15:30","15:30-16:00","16:00-16:30","16:30-17:00"
            };

            // Slot bị khóa do không có lịch làm
            var noSchedule = new List<string>();
            foreach (string slot in allSlots)
            {
                string ca = XacDinhCa(slot);
                bool coLich = ca == "Sang" ? coLichSang : coLichChieu;
                if (!coLich) noSchedule.Add(slot);
            }

            return Json(new { booked, noSchedule });
        }

        // ── POST: Lưu lịch hẹn ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(LichHen model)
        {
            var patient = GetCurrentPatient();
            if (patient == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            model.BenhNhanId = patient.Id;
            model.TrangThai = 0;
            model.CreatedAt = DateTime.Now;

            // Kiểm tra bác sĩ còn làm việc
            var doctor = _context.Doctors.Find(model.BacSiId);
            if (doctor == null || doctor.TrangThai != 1)
            {
                TempData["Error"] = "Bác sĩ hiện không làm việc, vui lòng chọn bác sĩ khác.";
                return RedirectToAction("Create");
            }

            // RÀNG BUỘC: kiểm tra bác sĩ có lịch làm việc trong ngày + ca đó không
            if (!BacSiCoLichLam(model.BacSiId, model.AppointmentDate, model.TimeSlot))
            {
                TempData["Error"] = "Bác sĩ không có lịch làm việc trong khung giờ này. Vui lòng chọn ngày hoặc giờ khác.";
                return RedirectToAction("Create");
            }

            // Kiểm tra slot đã đặt chưa
            bool slotTaken = _context.Appointments.Any(a =>
                a.BacSiId == model.BacSiId &&
                a.AppointmentDate.Date == model.AppointmentDate.Date &&
                a.TimeSlot == model.TimeSlot &&
                a.TrangThai != 3);

            if (slotTaken)
            {
                TempData["Error"] = "Khung giờ này đã có người đặt, vui lòng chọn giờ khác.";
                return RedirectToAction("Create");
            }

            _context.Appointments.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "Đặt lịch thành công! Chúng tôi sẽ liên hệ xác nhận sớm nhất.";
            return RedirectToAction("MyAppointment");
        }

        // ── DANH SÁCH LỊCH HẸN ──
        public IActionResult MyAppointment()
        {
            var patient = GetCurrentPatient();
            if (patient == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            var data = _context.Appointments
                .Where(a => a.BenhNhanId == patient.Id)
                .Include(a => a.BacSi)
                .Include(a => a.ChuyenKhoa)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
            return View(data);
        }

        // ── CHI TIẾT ──
        public IActionResult Detail(int id)
        {
            var patient = GetCurrentPatient();
            if (patient == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            var lich = _context.Appointments
                .Include(a => a.BacSi)
                .Include(a => a.ChuyenKhoa)
                .FirstOrDefault(a => a.Id == id && a.BenhNhanId == patient.Id);

            if (lich == null) return NotFound();
            return View(lich);
        }

        // ── HỦY LỊCH HẸN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var patient = GetCurrentPatient();
            if (patient == null) return Unauthorized();

            var lich = _context.Appointments
                .FirstOrDefault(a => a.Id == id && a.BenhNhanId == patient.Id);
            if (lich == null) return NotFound();

            if (lich.TrangThai == 2)
            {
                TempData["Error"] = "Lịch hẹn đã hoàn thành, không thể hủy.";
                return RedirectToAction("MyAppointment");
            }

            lich.TrangThai = 3;
            _context.SaveChanges();
            TempData["Success"] = "Đã hủy lịch hẹn thành công.";
            return RedirectToAction("MyAppointment");
        }

        // ── ĐỔI LỊCH ──
        public IActionResult Reschedule(int id)
        {
            var patient = GetCurrentPatient();
            if (patient == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            var lich = _context.Appointments
                .Include(a => a.BacSi)
                .Include(a => a.ChuyenKhoa)
                .FirstOrDefault(a => a.Id == id && a.BenhNhanId == patient.Id);

            if (lich == null) return NotFound();

            if (lich.TrangThai == 2 || lich.TrangThai == 3)
            {
                TempData["Error"] = "Lịch này không thể đổi.";
                return RedirectToAction("MyAppointment");
            }
            return View(lich);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reschedule(int id, DateTime appointmentDate,
                                         string timeSlot, string lyDoKham)
        {
            var patient = GetCurrentPatient();
            if (patient == null) return Unauthorized();

            var lich = _context.Appointments
                .FirstOrDefault(a => a.Id == id && a.BenhNhanId == patient.Id);
            if (lich == null) return NotFound();

            // RÀNG BUỘC: kiểm tra lịch làm việc cho ngày/giờ mới
            if (!BacSiCoLichLam(lich.BacSiId, appointmentDate, timeSlot))
            {
                TempData["Error"] = "Bác sĩ không có lịch làm việc trong khung giờ này.";
                return RedirectToAction("Reschedule", new { id });
            }

            bool slotTaken = _context.Appointments.Any(a =>
                a.Id != id &&
                a.BacSiId == lich.BacSiId &&
                a.AppointmentDate.Date == appointmentDate.Date &&
                a.TimeSlot == timeSlot &&
                a.TrangThai != 3);

            if (slotTaken)
            {
                TempData["Error"] = "Khung giờ này đã có người đặt.";
                return RedirectToAction("Reschedule", new { id });
            }

            lich.AppointmentDate = appointmentDate;
            lich.TimeSlot = timeSlot;
            lich.LyDoKham = lyDoKham;
            lich.TrangThai = 0;
            _context.SaveChanges();

            TempData["Success"] = "Đổi lịch thành công!";
            return RedirectToAction("MyAppointment");
        }
    }
}