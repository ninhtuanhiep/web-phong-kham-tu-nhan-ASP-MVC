using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services.Giao_diện;
using X.PagedList.Extensions;

namespace web_phong_kham_tu_nhan.Area.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LichHenController : Controller
    {
        private readonly IAppointmentService _service;
        private readonly ApplicationDbContext _context;

        public LichHenController(IAppointmentService service, ApplicationDbContext context)
        {
            _service = service;
            _context = context;
        }
        public IActionResult Index(int? TrangThai, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var lichHens = _context.Appointments
                .Include(x => x.BenhNhan)
                .Include(x => x.BacSi)
                .Include(x => x.ChuyenKhoa)
                .AsQueryable();

            ViewBag.AllAppointments = lichHens.Count(); 
            ViewBag.Pending = lichHens.Count(p => p.TrangThai == 0);
            ViewBag.Approved = lichHens.Count(p => p.TrangThai == 1);
            ViewBag.Completed = lichHens.Count(p => p.TrangThai == 2);

            if(TrangThai != null)
            {
                lichHens = lichHens.Where(p => p.TrangThai == TrangThai);
            }

            var pagedLichHen = lichHens
                .OrderBy(x => x.Id)
                .ToPagedList(pageNumber, pageSize);
                return View(pagedLichHen);

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
            ViewBag.Patients = new SelectList(_context.Patients, "Id", "FullName");
            ViewBag.Specialties = new SelectList(_context.Specialties, "Id", "Name");
            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "FullName");
            return View();
        }
        [HttpPost]
        public IActionResult Create(LichHen model)
        {
            if (ModelState.IsValid)
            {
                _service.Add(model);
                TempData["Success"] = "Thêm mới lịch hẹn thành công!";
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
            ViewBag.Patients = new SelectList(_context.Patients, "Id", "FullName", data.BenhNhanId);
            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "FullName", data.BacSiId);
            ViewBag.Specialties = new SelectList(_context.Specialties, "Id", "Name", data.ChuyenKhoaId);
            //ViewBag.TimeSlots = new SelectList(new List<string> { "08:00-09:00", "09:00-10:00", "10:00-11:00", "13:00-14:00", "14:00-15:00", "15:00-16:00" }, data.TimeSlot);
            return View(data);
        }
        //[HttpPost]
        //public IActionResult Edit(LichHen model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _service.Update(model);
        //        TempData["Success"] = "Cập nhật lịch hẹn thành công!";
        //        return RedirectToAction("Index");
        //    }
        //    return View(model);
        //}
        //[HttpPost]
        //public IActionResult Edit(LichHen model)
        //{
        //    // Bỏ qua validation của navigation properties
        //    ModelState.Remove("BenhNhan");
        //    ModelState.Remove("BacSi");
        //    ModelState.Remove("ChuyenKhoa");

        //    if (ModelState.IsValid)
        //    {
        //        _service.Update(model);
        //        TempData["Success"] = "Cập nhật lịch hẹn thành công!";
        //        return RedirectToAction("Index");
        //    }

        //    // Nếu vẫn lỗi, load lại ViewBag
        //    ViewBag.Patients = new SelectList(_context.Patients, "Id", "FullName", model.BenhNhanId);
        //    ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "FullName", model.BacSiId);
        //    ViewBag.Specialties = new SelectList(_context.Specialties, "Id", "Name", model.ChuyenKhoaId);
        //    return View(model);
        //}
        [HttpPost]
        public IActionResult Edit(LichHen model)
        {
            ModelState.Remove("BenhNhan");
            ModelState.Remove("BacSi");
            ModelState.Remove("ChuyenKhoa");
            ModelState.Remove("TimeSlot");

            // DEBUG: xem field nào lỗi
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });

                foreach (var error in errors)
                {
                    Console.WriteLine($"Field: {error.Field} - Errors: {string.Join(", ", error.Errors)}");
                }
            }

            if (ModelState.IsValid)
            {
                _service.Update(model);
                TempData["Success"] = "Cập nhật lịch hẹn thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.Patients = new SelectList(_context.Patients, "Id", "FullName", model.BenhNhanId);
            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "FullName", model.BacSiId);
            ViewBag.Specialties = new SelectList(_context.Specialties, "Id", "Name", model.ChuyenKhoaId);
            return View(model);
        }
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            TempData["success"] = "Xóa thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public JsonResult GetDoctorsBySpecialty(int specialtyId)
        {
            var doctors = _context.Doctors
                .Where(d => d.ChuyenKhoaId == specialtyId)
                .Select(d => new
                {
                    id = d.Id,
                    name = d.FullName
                })
                .ToList();

            return Json(doctors);
        }

    }
}
