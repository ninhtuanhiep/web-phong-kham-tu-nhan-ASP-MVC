using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services;
using BCrypt.Net;

namespace web_phong_kham_tu_nhan.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ── ĐĂNG KÝ ──
        public IActionResult Dangky() => View();

        [HttpPost]
        public IActionResult Dangky(string email, string password, string fullName, string phoneNumber)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "Email này đã được sử dụng.";
                return View();
            }

            var user = new User
            {
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Email = email,
                // ✅ HASH mật khẩu trước khi lưu
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                CreateAt = DateTime.Now,
                Role = "Bệnh nhân",
                IsActive = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            var patient = new BenhNhan
            {
                UserId = user.Id,
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                TrangThai = 1
            };

            _context.Patients.Add(patient);
            _context.SaveChanges();

            return RedirectToAction("Dangnhap");
        }

        // ── ĐĂNG NHẬP ──
        public IActionResult Dangnhap(bool pwChanged = false)
        {
            if (pwChanged) ViewBag.PwChanged = true;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Dangnhap(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            // ✅ Tìm user theo email trước, verify password sau (không tìm theo password trực tiếp)
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng.";
                return View();
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (user.Role == "Admin")
                return RedirectToAction("Index", "DashBoard", new { area = "Admin" });
            else if (user.Role == "Bác sĩ")
                return RedirectToAction("Index", "Profile", new { area = "BacSi" });
            else if (user.Role == "Bệnh nhân")
                return RedirectToAction("Index", "Home", new { area = "" });

            return RedirectToAction("Index", "Home");
        }

        // ── ĐĂNG XUẤT ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dangxuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("DangNhap", "Account");
        }

        // ── QUÊN MẬT KHẨU ──
        [HttpGet]
        public IActionResult QuenMatKhau() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuenMatKhau(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập địa chỉ email.";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.IsActive);
            if (user == null)
            {
                // Không tiết lộ email có tồn tại hay không (bảo mật)
                ViewBag.Success = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi mật khẩu mới.";
                return View();
            }

            string newPassword = TaoMatKhauNgauNhien();

            // ✅ Lưu password cũ để rollback nếu gửi email thất bại
            string oldPasswordHash = user.Password;

            // ✅ Hash mật khẩu mới trước khi lưu
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendResetPasswordAsync(user.Email, user.FullName ?? "Người dùng", newPassword);
                ViewBag.Success = "Mật khẩu mới đã được gửi đến email <strong>"
                    + email + "</strong>. Vui lòng kiểm tra hộp thư (kể cả Spam).";
            }
            catch (Exception)
            {
                // ✅ Rollback đúng cách: khôi phục hash cũ
                user.Password = oldPasswordHash;
                await _context.SaveChangesAsync();
                ViewBag.Error = "Không thể gửi email. Vui lòng liên hệ quản trị viên.";
            }

            return View();
        }

        private string TaoMatKhauNgauNhien()
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "@#!";

            // ✅ Dùng RandomNumberGenerator thay vì RNGCryptoServiceProvider (obsolete)
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);

            var result = new System.Text.StringBuilder();
            result.Append(chars[bytes[0] % chars.Length]);
            result.Append(digits[bytes[1] % digits.Length]);
            result.Append(special[bytes[2] % special.Length]);
            for (int i = 3; i < 8; i++)
                result.Append((chars + digits)[bytes[i] % (chars.Length + digits.Length)]);

            var arr = result.ToString().ToCharArray();
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = bytes[i % bytes.Length] % (i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return new string(arr);
        }
    }
}
