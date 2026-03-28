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

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(ChuyenKhoa model)
        {
            if (ModelState.IsValid)
            {
                _service.Add(model);
                TempData["success"] = "Thêm mới thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var sp = _service.GetById(id);
            return View(sp);
        }

        [HttpPost]
        public IActionResult Edit(ChuyenKhoa model)
        {
            _service.Update(model);
            TempData["success"] = "Cập nhật thành công!";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            TempData["success"] = "Xóa thành công!";
            return RedirectToAction("Index");
        }
    }
}
