using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Helpers;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Areas.BacSis.Controllers
{
    [Area("BacSi")]
    [Authorize(Roles = "Bác sĩ")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ An toàn: dùng TryParse, không throw nếu claim null
        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        private web_phong_kham_tu_nhan.Models.Entities.BacSi GetCurrentDoctor()
        {
            int userId = GetUserId();
            if (userId == 0) return null;

            return _context.Doctors
                .Include(d => d.ChuyenKhoa)
                .Include(d => d.HoSoBacSi)
                .Include(d => d.LichHens)
                .FirstOrDefault(d => d.UserId == userId);
        }

        // ── TRANG PROFILE TỔNG HỢP ──
        public IActionResult Index()
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null)
                return RedirectToAction("Dangnhap", "Account", new { area = "" });

            ViewBag.YeuCaus = _context.YeuCauCapNhats
                .Where(y => y.BacSiId == doctor.Id)
                .OrderByDescending(y => y.TaoLuc)
                .ToList();

            return View(doctor);
        }

        // ── CẬP NHẬT THÔNG TIN CƠ BẢN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInfo(
            string fullName, string email, string phoneNumber,
            string gioiTinh, string ngaySinh, string diaChi,
            string tieuSu, IFormFile imageFile)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            doctor.FullName = fullName;
            doctor.Email = email;
            doctor.DienThoai = phoneNumber;
            doctor.gioiTinh = gioiTinh;
            doctor.diaChi = diaChi;
            doctor.tieuSu = tieuSu;

            if (!string.IsNullOrEmpty(ngaySinh) && DateTime.TryParse(ngaySinh, out DateTime ngay))
                doctor.ngaySinh = ngay;

            // Upload ảnh với validation
            if (imageFile != null && imageFile.Length > 0)
            {
                string ext = Path.GetExtension(imageFile.FileName).ToLower();
                string[] allowed = { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowed.Contains(ext))
                {
                    TempData["Error"] = "Chỉ chấp nhận file ảnh JPG, PNG, WEBP.";
                    return RedirectToAction("Index");
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "File ảnh không được vượt quá 5MB.";
                    return RedirectToAction("Index");
                }

                string fileName = "doctor_" + doctor.Id + ext;
                string path = Path.Combine("wwwroot/Images/doctors", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using var stream = new FileStream(path, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                doctor.ImageUrl = "/Images/doctors/" + fileName;
            }

            // Đồng bộ sang User
            int userId = GetUserId();
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.FullName = fullName;
                user.Email = email;
                user.PhoneNumber = phoneNumber;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật thông tin thành công.";
            TempData["ActiveTab"] = "info";
            return RedirectToAction("Index");
        }

        // ── ĐỔI MẬT KHẨU ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword,
                                                         string newPassword,
                                                         string confirmPassword)
        {
            int userId = GetUserId();
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return Unauthorized();

            // ✅ Dùng BCrypt.Verify thay vì so sánh trực tiếp
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
            await _context.SaveChangesAsync();

            // ✅ Đăng xuất sau khi đổi mật khẩu (bảo mật)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Dangnhap", "Account", new { area = "", pwChanged = true });
        }

        // ── GỬI YÊU CẦU CẬP NHẬT CHUYÊN MÔN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiYeuCau(string tenTruong, string giaTriMoi, string lyDo)
        {
            var doctor = GetCurrentDoctor();
            if (doctor == null) return Unauthorized();

            if (string.IsNullOrEmpty(tenTruong) || string.IsNullOrEmpty(giaTriMoi))
            {
                TempData["Error"] = "Vui lòng chọn trường và nhập giá trị mới.";
                return RedirectToAction("Index");
            }

            bool daCo = _context.YeuCauCapNhats.Any(y =>
                y.BacSiId == doctor.Id && y.TenTruong == tenTruong && y.TrangThai == 0);

            if (daCo)
            {
                TempData["Error"] = "Bạn đã có yêu cầu cập nhật \"" + tenTruong + "\" đang chờ duyệt.";
                return RedirectToAction("Index");
            }

            string giaTriCu = "";
            if (doctor.HoSoBacSi != null)
            {
                giaTriCu = tenTruong switch
                {
                    "HocVi" => doctor.HoSoBacSi.HocVi ?? "",
                    "HocHam" => doctor.HoSoBacSi.HocHam ?? "",
                    "SoCCHN" => doctor.HoSoBacSi.SoCCHN ?? "",
                    "PhamViHanhNghe" => doctor.HoSoBacSi.PhamViHanhNghe ?? "",
                    "KinhNghiem" => doctor.HoSoBacSi.KinhNghiem ?? "",
                    "TruongDaoTao" => doctor.HoSoBacSi.TruongDaoTao ?? "",
                    "ChuyenMonSau" => doctor.HoSoBacSi.ChuyenMonSau ?? "",
                    "GiaiThuong" => doctor.HoSoBacSi.GiaiThuong ?? "",
                    "NgonNgu" => doctor.HoSoBacSi.NgonNgu ?? "",
                    "NamKinhNghiem" => doctor.HoSoBacSi.NamKinhNghiem.ToString(),
                    _ => ""
                };
            }

            _context.YeuCauCapNhats.Add(new YeuCauCapNhat
            {
                BacSiId = doctor.Id,
                TenTruong = tenTruong,
                GiaTriCu = giaTriCu,
                GiaTriMoi = giaTriMoi,
                LyDo = lyDo,
                TrangThai = 0,
                TaoLuc = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã gửi yêu cầu cập nhật. Admin sẽ xem xét sớm nhất.";
            TempData["ActiveTab"] = "yc";
            return RedirectToAction("Index");
        }
    }
}
