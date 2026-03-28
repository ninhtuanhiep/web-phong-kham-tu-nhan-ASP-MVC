using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;

namespace web_phong_kham_tu_nhan.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Dangky()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Dangky(string email, string password, string fullName, string phoneNumber)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }
            // Kiểm tra xem tài khoản đã tồn tại chưa
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (existingUser != null)
            {
                ViewBag.Error = "Tài khoản đã tồn tại.";
                return View();
            }
            // Tạo tài khoản mới
            var user = new User
            {
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Email = email,
                Password = password, // Lưu mật khẩu dưới dạng plain text (không an toàn, chỉ dùng cho demo)
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

        public IActionResult Dangnhap(bool pwChanged = false)
        {
            if (pwChanged)
                ViewBag.PwChanged = true;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Dangnhap(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }
            // Kiểm tra thông tin đăng nhập
            
            if (user == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng.";
                return View();
            }

            if(!user.IsActive)
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

            var indentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(indentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            //Dựa vào role để chuyển hướng đến trang phù hợp
            if(user.Role == "Admin")
            {
                return RedirectToAction("Index", "DashBoard", new { area = "Admin" });
            }
            else if(user.Role == "Bác sĩ")
            {
                return RedirectToAction("Index", "Profile", new { area = "BacSi" });

            }else if(user.Role == "Bệnh nhân")
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dangxuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("DangNhap","Account");
        }
    }  
}
