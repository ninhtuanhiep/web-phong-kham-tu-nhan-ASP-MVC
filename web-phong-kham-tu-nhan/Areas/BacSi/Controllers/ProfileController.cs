using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Areas.BacSis.Controllers
{
    [Area("BacSi")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        //private int GetUserId()
        //{
        //    var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        //    return claim != null ? int.Parse(claim.Value) : 0;
        //}
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
            {
                return id;
            }
            return 0;
        }

        //private Models.Entities.BacSi GetCurrentPatient()
        //{
        //    int userId = GetUserId();
        //    return _context.Doctors
        //        .Include(p => p.LichHens)
        //        .FirstOrDefault(p => p.UserId == userId);
        //}

        private Models.Entities.BacSi GetCurrentPatient()
        {
            int userId = GetUserId();
            var doctor = _context.Doctors
                .Include(d =>d.ChuyenKhoa)
                .Include(d => d.LichHens)
                .FirstOrDefault(d => d.UserId == userId);

            if (doctor != null)
            {
                // Chỉ giữ lại các lịch hẹn chưa bị hủy để hiển thị con số chính xác
                doctor.LichHens = doctor?.LichHens?.Where(lh => lh.TrangThai != 3).ToList();
            }

            return doctor;
        }

        private User GetCurrentUser()
        {
            int userId = GetUserId();
            return _context.Users.FirstOrDefault(u => u.Id == userId);
        }

        // ── TRANG CHỦ PROFILE ──
        public IActionResult Index()
        {
            var doctor = GetCurrentPatient();
            if (doctor == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });
            return View(doctor);
        }

        // ── CẬP NHẬT THÔNG TIN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateInfo(string fullName, string phoneNumber,
                                        string email, string diaChi,
                                        string gioiTinh, string ngaySinh,
                                        string tieuSu)
        {
            var doctor = GetCurrentPatient();
            var user = GetCurrentUser();
            if (doctor == null || user == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            doctor.FullName = fullName;
            doctor.DienThoai = phoneNumber;
            doctor.Email = email;
            doctor.diaChi = diaChi;
            doctor.gioiTinh = gioiTinh;
            doctor.tieuSu = tieuSu;

            if (!string.IsNullOrEmpty(ngaySinh) && DateTime.TryParse(ngaySinh, out DateTime ngay))
                doctor.ngaySinh = ngay;

            // Đồng bộ sang bảng User
            user.FullName = fullName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;

            _context.SaveChanges();


            TempData["Success"] = "Cập nhật thông tin thành công!";
            TempData["ActiveTab"] = "info";
            return RedirectToAction("Index");
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UpdateInfo(string fullName, string phoneNumber, string email, string diaChi, string gioiTinh, string ngaySinh, string tieuSu)
        //{
        //    int userId = GetUserId();
        //    var doctor = _context.Doctors.FirstOrDefault(p => p.UserId == userId);
        //    var user = _context.Users.FirstOrDefault(u => u.Id == userId);

        //    if (doctor == null || user == null)
        //    {
        //        return Content($"UserId {userId} không tìm thấy trong DB. Kiểm tra lại bảng Users và Doctors.");
        //    }

        //    try
        //    {
        //        doctor.FullName = fullName;
        //        doctor.DienThoai = phoneNumber;
        //        doctor.Email = email;
        //        doctor.diaChi = diaChi;
        //        doctor.gioiTinh = gioiTinh;
        //        doctor.tieuSu = tieuSu;
        //        if (!string.IsNullOrEmpty(ngaySinh) && DateTime.TryParse(ngaySinh, out DateTime ngay))
        //            doctor.ngaySinh = ngay;

        //        user.FullName = fullName;
        //        user.Email = email;
        //        user.PhoneNumber = phoneNumber;
        //        // user.UserName = email; // Bật dòng này nếu bạn đăng nhập bằng Email

        //        _context.SaveChanges();

        //        // Làm mới Cookie ngay lập tức
        //        var claims = new List<Claim> {
        //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        //    new Claim(ClaimTypes.Email, user.Email ?? ""),
        //    new Claim(ClaimTypes.Role, user.Role ?? ""),
        //    new Claim(ClaimTypes.Name, user.FullName ?? user.Email ?? "User")
        //};
        //        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        //        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        //        TempData["Success"] = "Cập nhật thành công!";
        //        return RedirectToAction("Index", "Profile", new { area = "BacSi" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Content("Lỗi khi lưu DB: " + ex.Message);
        //    }
        //}
        // ── ĐỔI MẬT KHẨU ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword,
                                             string newPassword,
                                             string confirmPassword)
        {
            var user = GetCurrentUser();
            if (user == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            // Kiểm tra các điều kiện
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                TempData["PwError"] = "Vui lòng nhập đầy đủ thông tin.";
                return View("Index");
            }

            if (user.Password != currentPassword)
            {
                TempData["PwError"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("Index");
            }

            if (newPassword != confirmPassword)
            {
                TempData["PwError"] = "Mật khẩu mới và xác nhận không khớp.";
                return RedirectToAction("Index");
            }

            if (newPassword.Length < 6)
            {
                TempData["PwError"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction("Index");
            }

            // 1. Lưu mật khẩu mới
            user.Password = newPassword;
            _context.SaveChanges();

            // 2. Đăng xuất
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 3. Redirect thẳng về Login với flag
            return RedirectToAction("Dangnhap", "Account", new { area = "", pwChanged = true });
        }
    }
}
