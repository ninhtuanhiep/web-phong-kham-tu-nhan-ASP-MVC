using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
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

        private Models.Entities.BenhNhan? GetCurrentPatient()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) return null;
            int userId = int.Parse(claim.Value);
            return _context.Patients.FirstOrDefault(p => p.UserId == userId);
        }

        // =========================
        // 1. FORM ĐẶT LỊCH
        // =========================
        public IActionResult Create()
        {
            ViewBag.ChuyenKhoas = new SelectList(_context.Specialties, "Id", "Name");
            return View();
        }

        // =========================
        // 2. XỬ LÝ ĐẶT LỊCH
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(LichHen model)
        {
            var patient = GetCurrentPatient();
            if (patient == null) return NotFound();

            model.BenhNhanId = patient.Id;
            model.TrangThai = 0;
            model.CreatedAt = DateTime.Now;

            var doctor = _context.Doctors.Find(model.BacSiId);
            if (doctor == null || doctor.TrangThai != 1)
            {
                TempData["Error"] = "Bác sĩ hiện không làm việc!";
                ViewBag.ChuyenKhoas = new SelectList(_context.Specialties, "Id", "Name");
                return View(model);
            }

            // Kiểm tra bác sĩ có lịch làm việc vào ngày đặt không
            var lichLamViec = _context.LichLamViecBacSis
                .FirstOrDefault(l => l.BacSiId == model.BacSiId
                                  && l.Ngay.Date == model.AppointmentDate.Date
                                  && l.TrangThai == 0);
            if (lichLamViec == null)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Bác sĩ không có lịch làm việc vào ngày này. Vui lòng chọn ngày khác.";
                ViewBag.ChuyenKhoas = new SelectList(_context.Specialties, "Id", "Name");
                return View(model);
            }

            // Kiểm tra time slot có thuộc ca làm việc của bác sĩ không
            var morningSlots = new[] { "07:30", "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00" };
            var afternoonSlots = new[] { "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30" };
            bool slotInShift = lichLamViec.CaLam == "CaNgay"
                ? morningSlots.Concat(afternoonSlots).Contains(model.TimeSlot)
                : lichLamViec.CaLam == "Sang"
                    ? morningSlots.Contains(model.TimeSlot)
                    : afternoonSlots.Contains(model.TimeSlot);

            if (!slotInShift)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Giờ khám không thuộc ca làm việc của bác sĩ trong ngày này.";
                ViewBag.ChuyenKhoas = new SelectList(_context.Specialties, "Id", "Name");
                return View(model);
            }

            if (model.AppointmentDate.Date == DateTime.Today)
            {
                var err = ValidateTimeSlot(model.TimeSlot);
                if (err != null)
                {
                    TempData["Error"] = err;
                    ViewBag.ChuyenKhoas = new SelectList(_context.Specialties, "Id", "Name");
                    return View(model);
                }
            }

            _context.Appointments.Add(model);
            _context.SaveChanges();
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Đặt lịch thành công! Vui lòng chờ bác sĩ xác nhận.";
            return RedirectToAction("MyAppointment");
        }
        // =========================
        // 3. XEM LỊCH CỦA TÔI
        // =========================
        public IActionResult MyAppointment()
        {
            var patient = GetCurrentPatient();
            if (patient == null) return NotFound();

            var data = _context.Appointments
                .Where(x => x.BenhNhanId == patient.Id && x.TrangThai != 3)
                .Include(x => x.BacSi)
                .Include(x => x.ChuyenKhoa)
                .OrderByDescending(x => x.AppointmentDate)
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

        // =========================
        // 4. AJAX: LOAD BÁC SĨ THEO CHUYÊN KHOA
        // =========================
        [HttpGet]
        public IActionResult GetDoctorsBySpecialty(int specialtyId)
        {
            var doctors = _context.Doctors
                .Where(x => x.ChuyenKhoaId == specialtyId && x.TrangThai == 1)
                .Select(x => new { id = x.Id, name = x.FullName, imageUrl = x.ImageUrl })
                .ToList();
            return Json(doctors);
        }

        // =========================
        // 5. AJAX: LẤY SLOT ĐÃ ĐẶT
        // =========================
        [HttpGet]
        public IActionResult GetBookedSlots(int bacSiId, string date)
        {
            if (!DateTime.TryParse(date, out DateTime ngay))
                return Json(new { hasSchedule = false, booked = new List<string>(), past = new List<string>() });

            // Kiểm tra bác sĩ có lịch làm việc vào ngày này không (TrangThai=0 nghĩa là "Làm việc")
            var lichLamViec = _context.LichLamViecBacSis
                .Where(l => l.BacSiId == bacSiId && l.Ngay.Date == ngay.Date && l.TrangThai == 0)
                .FirstOrDefault();

            if (lichLamViec == null)
                return Json(new { hasSchedule = false, booked = new List<string>(), past = new List<string>() });

            // Lọc slots theo ca làm việc của bác sĩ
            var morningSlots = new[] { "07:30", "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00" };
            var afternoonSlots = new[] { "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30" };

            List<string> availableSlots;
            if (lichLamViec.CaLam == "Sang")
                availableSlots = morningSlots.ToList();
            else if (lichLamViec.CaLam == "Chieu")
                availableSlots = afternoonSlots.ToList();
            else // CaNgay
                availableSlots = morningSlots.Concat(afternoonSlots).ToList();

            // Slots đã có người đặt
            var booked = _context.Appointments
                .Where(a => a.BacSiId == bacSiId
                         && a.AppointmentDate.Date == ngay.Date
                         && a.TrangThai != 3)
                .Select(a => a.TimeSlot)
                .ToList();

            // Slots đã qua (chỉ áp dụng hôm nay)
            var past = new List<string>();
            if (ngay.Date == DateTime.Today)
            {
                var now = DateTime.Now;
                foreach (var s in availableSlots)
                {
                    if (TimeSpan.TryParse(s, out var ts))
                    {
                        var slotTime = DateTime.Today.Add(ts);
                        // Khóa slot nếu còn dưới 30 phút
                        if (slotTime <= now.AddMinutes(30))
                            past.Add(s);
                    }
                }
            }

            return Json(new { hasSchedule = true, caLam = lichLamViec.CaLam, booked, past, availableSlots });
        }

        // =========================
        // 6. HỦY LỊCH
        // =========================
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Cancel(int id)
        //{
        //    var patient = GetCurrentPatient();
        //    if (patient == null) return NotFound();

        //    var lich = _context.Appointments
        //        .FirstOrDefault(x => x.Id == id && x.BenhNhanId == patient.Id);
        //    if (lich == null) return NotFound();

        //    // Chỉ hủy được khi chờ xác nhận (0) hoặc chờ xác nhận lại (4)
        //    if (lich.TrangThai != 0 && lich.TrangThai != 4)
        //    {
        //        TempData["Error"] = "Không thể hủy lịch đã được xác nhận.";
        //        return RedirectToAction("MyAppointment");
        //    }

        //    lich.TrangThai = 3;
        //    _context.SaveChanges();
        //    TempData["Success"] = "Đã hủy lịch hẹn.";
        //    return RedirectToAction("MyAppointment");
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var patient = GetCurrentPatient();
            if (patient == null) return NotFound();

            var lich = _context.Appointments
                .FirstOrDefault(x => x.Id == id && x.BenhNhanId == patient.Id);
            if (lich == null) return NotFound();

            if (lich.TrangThai != 0 && lich.TrangThai != 4)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Không thể hủy lịch này.";
                return RedirectToAction("MyAppointment");
            }

            lich.TrangThai = 3;
            _context.SaveChanges();
            TempData["ToastType"] = "warning";
            TempData["ToastMessage"] = "Đã hủy lịch hẹn thành công.";
            return RedirectToAction("MyAppointment");
        }

        // =========================
        // 7. FORM ĐỔI LỊCH
        // =========================

        [HttpGet]
        public IActionResult Reschedule(int id)
        {
            var patient = GetCurrentPatient();
            if (patient == null) return NotFound();

            var lich = _context.Appointments
                .Include(l => l.BacSi)
                .Include(l => l.ChuyenKhoa)
                .FirstOrDefault(l => l.Id == id && l.BenhNhanId == patient.Id);

            if (lich == null) return NotFound();

            if (lich.TrangThai != 0 && lich.TrangThai != 1)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Lịch hẹn này không thể đổi lịch.";
                return RedirectToAction("MyAppointment");
            }

            return View(lich);
        }

        // =========================
        // 8. XỬ LÝ ĐỔI LỊCH
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reschedule(int id, string newDate, string newTimeSlot, string rescheduleNote)
        {
            var patient = GetCurrentPatient();
            if (patient == null) return NotFound();

            var lich = _context.Appointments
                .FirstOrDefault(l => l.Id == id && l.BenhNhanId == patient.Id);
            if (lich == null) return NotFound();

            if (lich.TrangThai != 0 && lich.TrangThai != 1)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Lịch hẹn này không thể đổi lịch.";
                return RedirectToAction("MyAppointment");
            }

            if (!DateTime.TryParse(newDate, out DateTime ngayMoi))
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Ngày không hợp lệ.";
                return RedirectToAction("Reschedule", new { id });
            }

            if (ngayMoi.Date < DateTime.Today)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Không thể chọn ngày trong quá khứ.";
                return RedirectToAction("Reschedule", new { id });
            }

            // Kiểm tra bác sĩ có lịch làm việc vào ngày mới không
            var lichLamViec = _context.LichLamViecBacSis
                .FirstOrDefault(l => l.BacSiId == lich.BacSiId
                                  && l.Ngay.Date == ngayMoi.Date
                                  && l.TrangThai == 0);
            if (lichLamViec == null)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Bác sĩ không có lịch làm việc vào ngày này. Vui lòng chọn ngày khác.";
                return RedirectToAction("Reschedule", new { id });
            }

            // Kiểm tra time slot thuộc đúng ca làm việc
            var morningSlots = new[] { "07:30", "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00" };
            var afternoonSlots = new[] { "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30" };
            bool slotInShift = lichLamViec.CaLam == "CaNgay"
                ? morningSlots.Concat(afternoonSlots).Contains(newTimeSlot)
                : lichLamViec.CaLam == "Sang"
                    ? morningSlots.Contains(newTimeSlot)
                    : afternoonSlots.Contains(newTimeSlot);
            if (!slotInShift)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Giờ khám không thuộc ca làm việc của bác sĩ trong ngày đổi lịch.";
                return RedirectToAction("Reschedule", new { id });
            }

            if (ngayMoi.Date == DateTime.Today)
            {
                var err = ValidateTimeSlot(newTimeSlot);
                if (err != null)
                {
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = err;
                    return RedirectToAction("Reschedule", new { id });
                }
            }

            bool slotTaken = _context.Appointments.Any(a =>
                a.BacSiId == lich.BacSiId &&
                a.AppointmentDate.Date == ngayMoi.Date &&
                a.TimeSlot == newTimeSlot &&
                a.TrangThai != 3 &&
                a.Id != id);

            if (slotTaken)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Khung giờ này đã có người đặt, vui lòng chọn giờ khác.";
                return RedirectToAction("Reschedule", new { id });
            }

            lich.RescheduleDate = ngayMoi;
            lich.RescheduleTimeSlot = newTimeSlot;
            lich.RescheduleNote = rescheduleNote;
            lich.RescheduleAt = DateTime.Now;
            lich.TrangThai = 4;

            _context.SaveChanges();

            // ── THÔNG BÁO RÕ RÀNG CHO BỆNH NHÂN ──
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Đã gửi yêu cầu đổi lịch thành công!";
            TempData["RescheduleSuccess"] = "true"; // flag để hiện banner chi tiết
            TempData["RescheduleNewDate"] = ngayMoi.ToString("dd/MM/yyyy");
            TempData["RescheduleNewSlot"] = newTimeSlot;
            return RedirectToAction("MyAppointment");
        }

        // =========================
        // HELPER: VALIDATE GIỜ
        // =========================
        private string? ValidateTimeSlot(string? timeSlot)
        {
            if (string.IsNullOrEmpty(timeSlot)) return "Vui lòng chọn giờ khám.";
            var slotStart = timeSlot.Contains("-") ? timeSlot.Split('-')[0].Trim() : timeSlot.Trim();
            if (!TimeSpan.TryParse(slotStart, out var ts)) return "Giờ khám không hợp lệ.";
            if (DateTime.Today.Add(ts) <= DateTime.Now.AddMinutes(30))
                return "Khung giờ này đã qua hoặc quá gần. Vui lòng chọn giờ khác (ít nhất 30 phút).";
            return null;
        }
    }
}
