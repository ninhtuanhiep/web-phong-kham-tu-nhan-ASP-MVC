using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Models.ViewModel;

namespace web_phong_kham_tu_nhan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        //public IActionResult Index()
        //{
        //    var doctor = _context.Doctors
        //        .Include(d => d.ChuyenKhoa)
        //        .Take(6)
        //        .Where(x => x.TrangThai !=3)
        //        .ToList();
        //    var chuyenKhoas = _context.Specialties.ToList();
        //    var bacSis = _context.Doctors.ToList();

        //    ViewBag.ChuyenKhoas = new SelectList(chuyenKhoas, "Id", "Name");
        //    ViewBag.BacSis = new SelectList(bacSis, "Id", "FullName");

        //    return View(doctor);
        //}
        public IActionResult Index()
        {
            var doctors = _context.Doctors
                .Include(d => d.ChuyenKhoa)
                .Where(x => x.TrangThai != 3)
                .Take(6)
                .ToList();

            var chuyenKhoas = _context.Specialties.ToList();
            var bacSis = _context.Doctors.ToList();

            ViewBag.ChuyenKhoas = new SelectList(chuyenKhoas, "Id", "Name");
            ViewBag.BacSis = new SelectList(bacSis, "Id", "FullName");

            // 🔥 TẠO VIEWMODEL
            var vm = new HomeViewModel
            {
                Doctors = doctors,
                LichHen = new LichHen()
            };

            return View(vm); // ✅ trả đúng kiểu
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
