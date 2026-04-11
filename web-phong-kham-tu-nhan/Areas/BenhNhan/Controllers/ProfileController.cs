using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Helpers;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Areas.BenhNhans.Controllers
{
    [Area("BenhNhan")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        private web_phong_kham_tu_nhan.Models.Entities.BenhNhan GetCurrentPatient()
        {
            int userId = GetUserId();
            if (userId == 0) return null;
            return _context.Patients
                .Include(p => p.LichHens)
                .FirstOrDefault(p => p.UserId == userId);
        }

        private User GetCurrentUser()
        {
            int userId = GetUserId();
            if (userId == 0) return null;
            return _context.Users.FirstOrDefault(u => u.Id == userId);
        }

        public IActionResult Index()
        {
            var patient = GetCurrentPatient();
            if (patient == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateInfo(string fullName, string phoneNumber,
                                        string email, string diaChi,
                                        string gioiTinh, string ngaySinh,
                                        string lichSuYTe)
        {
            var patient = GetCurrentPatient();
            var user = GetCurrentUser();
            if (patient == null || user == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            patient.FullName = fullName;
            patient.PhoneNumber = phoneNumber;
            patient.Email = email;
            patient.DiaChi = diaChi;
            patient.GioiTinh = gioiTinh;
            patient.LichSuYTe = lichSuYTe;

            if (!string.IsNullOrEmpty(ngaySinh) && DateTime.TryParse(ngaySinh, out DateTime ngay))
                patient.NgaySinh = ngay;

            user.FullName = fullName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;

            _context.SaveChanges();
            TempData["Success"] = "Cập nhật thông tin thành công!";
            TempData["ActiveTab"] = "info";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword,
                                                         string newPassword,
                                                         string confirmPassword)
        {
            var user = GetCurrentUser();
            if (user == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            // ✅ Dùng BCrypt.Verify
            if (!PasswordHelper.Verify(currentPassword, user.Password))
            {
                TempData["PwError"] = "Mật khẩu hiện tại không đúng.";
                TempData["ActiveTab"] = "password";
                return RedirectToAction("Index");
            }

            if (newPassword != confirmPassword)
            {
                TempData["PwError"] = "Mật khẩu mới và xác nhận không khớp.";
                TempData["ActiveTab"] = "password";
                return RedirectToAction("Index");
            }

            if (newPassword.Length < 6)
            {
                TempData["PwError"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                TempData["ActiveTab"] = "password";
                return RedirectToAction("Index");
            }

            // ✅ Hash mật khẩu mới
            user.Password = PasswordHelper.Hash(newPassword);
            _context.SaveChanges();

            // ✅ Đăng xuất sau khi đổi mật khẩu
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Dangnhap", "Account", new { area = "", pwChanged = true });
        }
    }
}
