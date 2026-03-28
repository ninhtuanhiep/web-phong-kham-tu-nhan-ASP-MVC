using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services.Giao_diện;
using X.PagedList.Extensions;

namespace web_phong_kham_tu_nhan.Area.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BenhNhanController : Controller
    {
        private readonly IPatientService _service;
        private readonly ApplicationDbContext _context;

        public BenhNhanController(IPatientService service, ApplicationDbContext context)
        {
            _service = service;
            _context = context;

        }
        public IActionResult Index(int? TrangThai, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var patients = _context.Patients.AsQueryable();
            ViewBag.AllPatients = patients.Count();
            ViewBag.ActivePatients = patients.Count(p => p.TrangThai == 1);
            ViewBag.InactivePatients = patients.Count(p => p.TrangThai == 0);

            if(TrangThai != null)
            {
                patients = patients.Where(p => p.TrangThai == TrangThai);
            }

            var pagedPatients = patients
                .OrderBy(x => x.Id)
                .ToPagedList(pageNumber, pageSize);

            return View(pagedPatients);
        }

        public IActionResult Detail(int id)
        {
            var data = _service.GetById(id);
            if (data == null)
            {
                return NotFound();
            }
            return View(data);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(BenhNhan model)
        {
            if (ModelState.IsValid)
            {
                _service.Add(model);
                TempData["Success"] = "Thêm mới bệnh nhân thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var data = _service.GetById(id);
            if (data == null)
            {
                return NotFound();
            }
            return View(data);
        }

        [HttpPost]
        public IActionResult Edit(BenhNhan model)
        {
            if (ModelState.IsValid)
            {
                _service.Update(model);
                TempData["Success"] = "Cập nhật bệnh nhân thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            TempData["success"] = "Xóa thành công!";
            return RedirectToAction("Index");
        }
    }
}
