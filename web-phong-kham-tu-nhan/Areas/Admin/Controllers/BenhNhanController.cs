using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services;
using web_phong_kham_tu_nhan.Services.Triển_khai;
using X.PagedList.Extensions;

namespace web_phong_kham_tu_nhan.Area.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BenhNhanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public BenhNhanController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ── DANH SÁCH ──
        public IActionResult Index(int? TrangThai, int? page, string search)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var patients = _context.Patients.AsQueryable();

            ViewBag.AllPatients = patients.Count();
            ViewBag.ActivePatients = patients.Count(p => p.TrangThai == 1);
            ViewBag.InactivePatients = patients.Count(p => p.TrangThai == 0);

            if (TrangThai != null)
                patients = patients.Where(p => p.TrangThai == TrangThai);

            if (!string.IsNullOrEmpty(search))
                patients = patients.Where(p =>
                    p.FullName.Contains(search) ||
                    p.PhoneNumber.Contains(search) ||
                    p.Email.Contains(search));

            ViewBag.Search = search;
            ViewBag.TrangThai = TrangThai;

            var pagedPatients = patients
                .OrderByDescending(x => x.Id)
                .ToPagedList(pageNumber, pageSize);

            return View(pagedPatients);
        }

        // ── CHI TIẾT ──
        public IActionResult Detail(int id)
        {
            var patient = _context.Patients
                .Include(p => p.LichHens)
                    .ThenInclude(l => l.BacSi)
                .FirstOrDefault(p => p.Id == id);

            if (patient == null) return NotFound();

            // Lấy User liên kết
            ViewBag.User = _context.Users.FirstOrDefault(u => u.Id == patient.UserId);
            return View(patient);
        }

        // ── FORM THÊM MỚI ──
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ── LƯU BỆNH NHÂN MỚI + TẠO TÀI KHOẢN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string fullName, string email, string phoneNumber,
            string gioiTinh, string ngaySinh, string diaChi,
            string lichSuYTe, string matKhau)
        {
            // Validate
            if (string.IsNullOrEmpty(fullName))
            {
                ViewBag.Error = "Họ tên không được để trống.";
                return View();
            }

            // Kiểm tra email trùng
            if (!string.IsNullOrEmpty(email) && _context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "Email " + email + " đã được sử dụng.";
                return View();
            }

            // 1. Tạo User
            string mkMacDinh = string.IsNullOrEmpty(matKhau)
                ? "Benhnhan@" + DateTime.Now.Year
                : matKhau;

            var user = new User
            {
                FullName = fullName,
                Email = email,
                Password = mkMacDinh,
                PhoneNumber = phoneNumber,
                Role = "Bệnh nhân",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 2. Tạo BenhNhan liên kết với User
            var benhNhan = new BenhNhan
            {
                UserId = user.Id,
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                GioiTinh = gioiTinh,
                DiaChi = diaChi,
                LichSuYTe = lichSuYTe,
                TrangThai = 1
            };

            if (!string.IsNullOrEmpty(ngaySinh) && DateTime.TryParse(ngaySinh, out DateTime ngay))
                benhNhan.NgaySinh = ngay;

            _context.Patients.Add(benhNhan);
            await _context.SaveChangesAsync();

            // 3. Gửi email thông báo tài khoản (nếu có email)
            if (!string.IsNullOrEmpty(email))
            {
                try
                {
                    await _emailService.SendWelcomePatientAsync(email, fullName, email, mkMacDinh);
                }
                catch
                {
                    // Không block nếu gửi email thất bại
                }
            }

            TempData["Success"] = "Đã thêm bệnh nhân " + fullName
                + " | Tài khoản: " + email
                + " | Mật khẩu: " + mkMacDinh;
            return RedirectToAction("Index");
        }

        // ── CHỈNH SỬA ──
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var patient = _context.Patients.FirstOrDefault(p => p.Id == id);
            if (patient == null) return NotFound();
            ViewBag.User = _context.Users.FirstOrDefault(u => u.Id == patient.UserId);
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string fullName, string email,
            string phoneNumber, string gioiTinh, string ngaySinh,
            string diaChi, string lichSuYTe, int trangThai)
        {
            var patient = _context.Patients.FirstOrDefault(p => p.Id == id);
            if (patient == null) return NotFound();

            patient.FullName = fullName;
            patient.Email = email;
            patient.PhoneNumber = phoneNumber;
            patient.GioiTinh = gioiTinh;
            patient.DiaChi = diaChi;
            patient.LichSuYTe = lichSuYTe;
            patient.TrangThai = trangThai;

            if (!string.IsNullOrEmpty(ngaySinh) && DateTime.TryParse(ngaySinh, out DateTime ngay))
                patient.NgaySinh = ngay;

            // Đồng bộ User
            var user = _context.Users.FirstOrDefault(u => u.Id == patient.UserId);
            if (user != null)
            {
                user.FullName = fullName;
                user.Email = email;
                user.PhoneNumber = phoneNumber;
                user.IsActive = trangThai == 1;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật thông tin bệnh nhân " + fullName;
            return RedirectToAction("Detail", new { id });
        }

        // ── VÔ HIỆU HÓA ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var patient = _context.Patients.FirstOrDefault(p => p.Id == id);
            if (patient == null) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == patient.UserId);
            if (user != null) user.IsActive = false;

            patient.TrangThai = 0;
            _context.SaveChanges();

            TempData["Success"] = "Đã vô hiệu hóa tài khoản bệnh nhân " + patient.FullName;
            return RedirectToAction("Index");
        }

        // ── RESET MẬT KHẨU ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetMatKhau(int id)
        {
            var patient = _context.Patients.FirstOrDefault(p => p.Id == id);
            if (patient == null) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == patient.UserId);
            if (user == null)
            {
                TempData["Error"] = "Bệnh nhân này chưa có tài khoản.";
                return RedirectToAction("Detail", new { id });
            }

            string newPassword = TaoMatKhauNgauNhien();
            user.Password = newPassword;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    await _emailService.SendResetPasswordAsync(
                        user.Email, patient.FullName ?? "Bệnh nhân", newPassword);
                    TempData["Success"] = "Đã reset mật khẩu và gửi email cho " + patient.FullName;
                }
                catch
                {
                    TempData["Error"] = "Reset thành công nhưng không thể gửi email. Mật khẩu mới: " + newPassword;
                }
            }
            else
            {
                TempData["Success"] = "Đã reset mật khẩu. Mật khẩu mới: " + newPassword;
            }

            return RedirectToAction("Detail", new { id });
        }

        private string TaoMatKhauNgauNhien()
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "@#!";
            var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var bytes = new byte[8];
            rng.GetBytes(bytes);
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
                var tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
            }
            return new string(arr);
        }
    }
}
