using web_phong_kham_tu_nhan.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using web_phong_kham_tu_nhan.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace web_phong_kham_tu_nhan.Area.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashBoardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashBoardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            // Cards
            ViewBag.TotalPatients = _context.Patients.Count();
            ViewBag.TotalDoctors = _context.Doctors.Count();
            ViewBag.TotalAppointments = _context.Appointments.Count();

            ViewBag.Pending = _context.Appointments.Count(x => x.TrangThai == 0);
            ViewBag.Approved = _context.Appointments.Count(x => x.TrangThai == 1);
            ViewBag.Completed = _context.Appointments.Count(x => x.TrangThai == 2);

            // Lịch hôm nay
            var today = DateTime.Today;

            ViewBag.TodayAppointments = _context.Appointments
                .Include(x => x.BenhNhan)
                .Include(x => x.BacSi)
                .Where(x => x.AppointmentDate.Date == today)
                .Take(5)
                .ToList();

            // Top bác sĩ nhiều lịch nhất
            var topDoctors = _context.Appointments
                .Include(x => x.BacSi)
                .GroupBy(x => x.BacSi)
                .Select(g => new
                {
                    DoctorName = g.Key.FullName,
                    Total = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            ViewBag.TopDoctors = topDoctors;

            // Thống kê lịch theo tháng
            var monthlyStats = _context.Appointments
                .GroupBy(x => x.AppointmentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            ViewBag.MonthlyStats = monthlyStats;

            return View();
        }
    }
}
