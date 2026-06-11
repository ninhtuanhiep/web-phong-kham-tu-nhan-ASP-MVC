using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Models.ViewModel;
using web_phong_kham_tu_nhan.Services.Giao_diện;

namespace web_phong_kham_tu_nhan.Area.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChuyenKhoaController : Controller
    {
        private readonly ISpecialtyService _service;
        private readonly ApplicationDbContext _context;

        public ChuyenKhoaController(ISpecialtyService service, ApplicationDbContext context)
        {
            _service = service;
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.Specialties
                .Select(s => new ChuyenKhoaVM
                {
                    Id = s.Id,
                    Name = s.Name,
                    MoTa = s.MoTa,
                    DoctorCount = s.BacSis.Count(),
                    PatientCount = _context.Appointments
                        .Where(a => a.BacSi.ChuyenKhoaId == s.Id)
                        .Select(a => a.BenhNhanId)
                        .Distinct()
                        .Count()
                })
                .ToList();

            return View(data);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]   // ✅ Thêm AntiForgery
        public IActionResult Create(ChuyenKhoa model)
        {
            if (ModelState.IsValid)
            {
                _service.Add(model);
                TempData["Success"] = "Thêm mới chuyên khoa thành công!";  // ✅ chữ hoa
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var sp = _service.GetById(id);
            if (sp == null) return NotFound();
            return View(sp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]   // ✅ Thêm AntiForgery
        public IActionResult Edit(ChuyenKhoa model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _service.Update(model);
            TempData["Success"] = "Cập nhật chuyên khoa thành công!";  // ✅ chữ hoa
            return RedirectToAction("Index");
        }

        // ✅ Đổi sang HttpPost + AntiForgery để tránh CSRF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            // Kiểm tra còn bác sĩ thuộc chuyên khoa này không
            bool cosBacSi = _context.Doctors.Any(d => d.ChuyenKhoaId == id);
            if (cosBacSi)
            {
                TempData["Error"] = "Không thể xóa chuyên khoa đang có bác sĩ thuộc về.";
                return RedirectToAction("Index");
            }

            _service.Delete(id);
            TempData["Success"] = "Xóa chuyên khoa thành công!";  // ✅ chữ hoa
            return RedirectToAction("Index");
        }
    }
}
