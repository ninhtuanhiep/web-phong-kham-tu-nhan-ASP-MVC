using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services.Giao_diện;
public class DoctorController : Controller
{
    private readonly IDoctorServices _service;
    private readonly ApplicationDbContext _context;

    public DoctorController(IDoctorServices service, ApplicationDbContext context )
    {
        _service = service;
        _context = context;
    }

    public List<BacSi> GetActiveDoctors()
    {
        return _context.Doctors
            .Include(x => x.ChuyenKhoa)
            .Where(x => x.TrangThai !=3) // 🔥 CHỈ LẤY ĐANG LÀM và nghỉ phép, bỏ qua đã nghỉ việc
            .ToList();
    }

    public IActionResult Index()
    {
        var doctors = GetActiveDoctors();

        var chuyenKhoa = doctors
            .Where(x => x.ChuyenKhoa != null)
            .Select(x => x.ChuyenKhoa)
            .Distinct()
            .ToList();
        ViewBag.ChuyenKhoas = chuyenKhoa;
        return View(doctors);
    }
}