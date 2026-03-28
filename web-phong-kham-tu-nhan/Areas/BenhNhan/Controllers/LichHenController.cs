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

        // GET: Trang dat lich
        public IActionResult Create()
        {
            ViewBag.ChuyenKhoas = _context.Specialties.ToList();
            return View();
        }

        // AJAX: Lay bac si theo chuyen khoa
        public IActionResult GetDoctorsBySpecialty(int specialtyId)
        {
            var doctors = _context.Doctors
                .Where(d => d.ChuyenKhoaId == specialtyId && d.TrangThai == 1)
                .Select(d => new
                {
                    id = d.Id,
                    fullName = d.FullName,
                    imageUrl = d.ImageUrl != null ? d.ImageUrl : "/Images/default-avatar.png",
                    tieuSu = d.tieuSu != null ? d.tieuSu : "Bac si chuyen khoa"
                })
                .ToList();
            return Json(doctors);
        }

        // AJAX: Lay slot da dat
        public IActionResult GetBookedSlots(int doctorId, string date)
        {
            DateTime parsedDate;
            if (!DateTime.TryParse(date, out parsedDate))
                return Json(new List<string>());

            var booked = _context.Appointments
                .Where(a => a.BacSiId == doctorId
                         && a.AppointmentDate.Date == parsedDate.Date
                         && a.TrangThai != 3)
                .Select(a => a.TimeSlot)
                .ToList();
            return Json(booked);
        }

        // POST: Luu lich hen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(LichHen model)
        {
            var patient = GetCurrentPatient();
            if (patient == null)
                return RedirectToAction("DangNhap", "Account", new { area = "" });

            model.BenhNhanId = patient.Id;
            model.TrangThai = 1;
            model.CreatedAt = DateTime.Now;

            var doctor = _context.Doctors.Find(model.BacSiId);
            if (doctor == null || doctor.TrangThai != 1)
            {
                TempData["Error"] = "Bác sĩ hiện không làm việc, vui lòng chọn bác sĩ khác";
                return RedirectToAction("Create");
            }

            bool slotTaken = _context.Appointments.Any(a =>
                a.BacSiId == model.BacSiId &&
                a.AppointmentDate.Date == model.AppointmentDate.Date &&
                a.TimeSlot == model.TimeSlot &&
                a.TrangThai != 3);

            if (slotTaken)
            {
                TempData["Error"] = "Khung giờ này đã có người đặt, vui lòng chọn khung giờ khác.";
                return RedirectToAction("Create");
            }

            _context.Appointments.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "Đặt lịch thành công, chúng tôi sẽ liên hệ với bạn trong thời gian sớm nhất.";
            return RedirectToAction("MyAppointment");
        }

        // GET: Danh sach lich hen
        public IActionResult MyAppointment()
        {
            var patient = GetCurrentPatient();
            if (patient == null)
                return RedirectToAction("DangNhap", "Account", new { area = "" });

            var data = _context.Appointments
                .Where(a => a.BenhNhanId == patient.Id)
                .Include(a => a.BacSi)
                .Include(a => a.ChuyenKhoa)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
            return View(data);
        }

        // GET: Chi tiet lich hen
        public IActionResult Detail(int id)
        {
            var patient = GetCurrentPatient();
            if (patient == null)
                return RedirectToAction("DangNhap", "Account", new { area = "" });

            var lich = _context.Appointments
                .Include(a => a.BacSi)
                .Include(a => a.ChuyenKhoa)
                .FirstOrDefault(a => a.Id == id && a.BenhNhanId == patient.Id);

            if (lich == null) return NotFound();
            return View(lich);
        }

        // POST: Huy lich hen
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
                TempData["Error"] = "Lịch hẹn đã hoàn thành không thể hủy.";
                return RedirectToAction("MyAppointment");
            }

            lich.TrangThai = 3;
            _context.SaveChanges();
            TempData["Success"] = "Đã hủy lịch hẹn thành công.";
            return RedirectToAction("MyAppointment");
        }

        // GET: Form doi lich
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

        // POST: Luu doi lich
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reschedule(int id, DateTime appointmentDate, string timeSlot, string lyDoKham)
        {
            var patient = GetCurrentPatient();
            if (patient == null) return Unauthorized();

            var lich = _context.Appointments
                .FirstOrDefault(a => a.Id == id && a.BenhNhanId == patient.Id);

            if (lich == null) return NotFound();

            bool slotTaken = _context.Appointments.Any(a =>
                a.Id != id &&
                a.BacSiId == lich.BacSiId &&
                a.AppointmentDate.Date == appointmentDate.Date &&
                a.TimeSlot == timeSlot &&
                a.TrangThai != 3);

            if (slotTaken)
            {
                TempData["Error"] = "Khung giờ này đã có người đặt, vui lòng chọn khung giờ khác.";
                return RedirectToAction("Reschedule", new { id });
            }

            lich.AppointmentDate = appointmentDate;
            lich.TimeSlot = timeSlot;
            lich.LyDoKham = lyDoKham;
            lich.TrangThai = 0;
            _context.SaveChanges();

            TempData["Success"] = "Đổi lịch thành công, chúng tôi sẽ xác nhận lại trong thời gian sớm nhất.";
            return RedirectToAction("MyAppointment");
        }
    }
}