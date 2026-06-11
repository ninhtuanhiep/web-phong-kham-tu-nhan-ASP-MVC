using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Helpers;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services;
using web_phong_kham_tu_nhan.Services.Giao_diện;
using X.PagedList.Extensions;

namespace web_phong_kham_tu_nhan.Area.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BacSiController : Controller
    {
        private readonly IDoctorServices _service;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public BacSiController(IDoctorServices service, IWebHostEnvironment env,
            ApplicationDbContext context, EmailService emailService)
        {
            _service = service;
            _env = env;
            _context = context;
            _emailService = emailService;
        }

        // ── DANH SÁCH ──
        public IActionResult Index(int? TrangThai, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var doctor = _context.Doctors
                .Include(d => d.ChuyenKhoa)
                .Include(d => d.LichHens)
                .Include(d => d.HoSoBacSi)
                .AsQueryable();

            ViewBag.AllDoctor = doctor.Count();
            ViewBag.ActiveDoctor = doctor.Count(p => p.TrangThai == 1);
            ViewBag.InactiveDoctor = doctor.Count(p => p.TrangThai == 0);
            ViewBag.OnLeaveDoctor = doctor.Count(p => p.TrangThai == 2);
            ViewBag.PendingDoctor = doctor.Count(p => p.TrangThai == 3);

            if (TrangThai != null)
                doctor = doctor.Where(p => p.TrangThai == TrangThai);

            var pagedDoctor = doctor.OrderBy(x => x.Id).ToPagedList(pageNumber, pageSize);
            return View(pagedDoctor);
        }

        // ── FORM TẠO MỚI ──
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.ChuyenKhoas = new SelectList(
                _context.Specialties.OrderBy(c => c.Name), "Id", "Name");
            return View();
        }

        // ── LƯU BÁC SĨ MỚI + TẠO TÀI KHOẢN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string fullName, string email, string dienThoai,
            string gioiTinh, DateTime? ngaySinh, int chuyenKhoaId,
            string tieuSu, IFormFile imageFile, string diaChi,
            string hocVi, string hocHam, string soCCHN,
            string phamViHanhNghe, string ngayCapCCHN, string ngayHetHanCCHN,
            string kinhNghiem, int namKinhNghiem,
            string truongDaoTao, string chuyenMonSau,
            string ngonNgu, string giaiThuong,
            string matKhau)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["Error"] = "Email " + email + " đã được sử dụng.";
                ViewBag.ChuyenKhoas = new SelectList(
                    _context.Specialties.OrderBy(c => c.Name), "Id", "Name");
                return View();
            }

            // ✅ Tạo mật khẩu và hash ngay — không lưu plain text
            string mkPlainText = string.IsNullOrEmpty(matKhau)
                ? PasswordHelper.GenerateRandom()
                : matKhau;

            var user = new User
            {
                FullName = fullName,
                Email = email,
                Password = PasswordHelper.Hash(mkPlainText),   // ✅ BCrypt hash
                PhoneNumber = dienThoai,
                Role = "Bác sĩ",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Upload ảnh với validation
            string imageUrl = "/Images/default-avatar.png";
            if (imageFile != null && imageFile.Length > 0)
            {
                string ext = Path.GetExtension(imageFile.FileName).ToLower();
                string[] allowed = { ".jpg", ".jpeg", ".png", ".webp" };
                if (allowed.Contains(ext) && imageFile.Length <= 5 * 1024 * 1024)
                {
                    string fileName = "doctor_" + user.Id + ext;
                    string path = Path.Combine("wwwroot/Images/doctors", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = new FileStream(path, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    imageUrl = "/Images/doctors/" + fileName;
                }
            }

            var bacSi = new BacSi
            {
                FullName = fullName,
                Email = email,
                DienThoai = dienThoai,
                gioiTinh = gioiTinh,
                ngaySinh = ngaySinh,
                diaChi = diaChi,
                ImageUrl = imageUrl,
                tieuSu = tieuSu,
                ChuyenKhoaId = chuyenKhoaId,
                TrangThai = 1,
                UserId = user.Id
            };
            _context.Doctors.Add(bacSi);
            await _context.SaveChangesAsync();

            var hoSo = new HoSoBacSi
            {
                BacSiId = bacSi.Id,
                HocVi = hocVi,
                HocHam = hocHam,
                SoCCHN = soCCHN,
                PhamViHanhNghe = phamViHanhNghe,
                NgayCapCCHN = string.IsNullOrEmpty(ngayCapCCHN) ? null
                                 : DateTime.TryParse(ngayCapCCHN, out var d1) ? d1 : null,
                NgayHetHanCCHN = string.IsNullOrEmpty(ngayHetHanCCHN) ? null
                                 : DateTime.TryParse(ngayHetHanCCHN, out var d2) ? d2 : null,
                KinhNghiem = kinhNghiem,
                NamKinhNghiem = namKinhNghiem,
                TruongDaoTao = truongDaoTao,
                ChuyenMonSau = chuyenMonSau,
                NgonNgu = ngonNgu,
                GiaiThuong = giaiThuong,
                CapNhatLuc = DateTime.Now
            };
            _context.HoSoBacSis.Add(hoSo);
            await _context.SaveChangesAsync();

            // ✅ Gửi email thông tin đăng nhập thay vì hiển thị mật khẩu trên màn hình
            try
            {
                await _emailService.SendWelcomePatientAsync(
                    email, fullName, email, mkPlainText);
            }
            catch { /* gửi thất bại vẫn cho tạo thành công */ }

            TempData["Success"] = "Đã thêm BS. " + fullName
                + ". Thông tin đăng nhập đã được gửi qua email " + email + ".";
            return RedirectToAction("Index");
        }

        // ── XEM CHI TIẾT HỒ SƠ ──
        public IActionResult Detail(int id)
        {
            var bacSi = _context.Doctors
                .Include(b => b.ChuyenKhoa)
                .Include(b => b.HoSoBacSi)
                .Include(b => b.LichHens)
                .FirstOrDefault(b => b.Id == id);

            if (bacSi == null) return NotFound();

            ViewBag.YeuCaus = _context.YeuCauCapNhats
                .Where(y => y.BacSiId == id)
                .OrderByDescending(y => y.TaoLuc)
                .ToList();

            ViewBag.ChuyenKhoas = new SelectList(
                _context.Specialties.OrderBy(c => c.Name), "Id", "Name", bacSi.ChuyenKhoaId);

            return View(bacSi);
        }

        // ── SỬA THÔNG TIN CƠ BẢN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string fullName, string email,
            string dienThoai, string tieuSu, int chuyenKhoaId,
            int trangThai, IFormFile imageFile)
        {
            var bacSi = _context.Doctors
                .Include(b => b.HoSoBacSi)
                .FirstOrDefault(b => b.Id == id);
            if (bacSi == null) return NotFound();

            bacSi.FullName = fullName;
            bacSi.Email = email;
            bacSi.DienThoai = dienThoai;
            bacSi.tieuSu = tieuSu;
            bacSi.ChuyenKhoaId = chuyenKhoaId;
            bacSi.TrangThai = trangThai;

            if (imageFile != null && imageFile.Length > 0)
            {
                string ext = Path.GetExtension(imageFile.FileName).ToLower();
                string[] allowed = { ".jpg", ".jpeg", ".png", ".webp" };
                if (allowed.Contains(ext) && imageFile.Length <= 5 * 1024 * 1024)
                {
                    string fileName = "doctor_" + id + ext;
                    string path = Path.Combine("wwwroot/Images/doctors", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = new FileStream(path, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    bacSi.ImageUrl = "/Images/doctors/" + fileName;
                }
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == bacSi.UserId);
            if (user != null)
            {
                user.FullName = fullName;
                user.Email = email;
                user.PhoneNumber = dienThoai;
                user.IsActive = trangThai == 1;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật thông tin BS. " + fullName;
            return RedirectToAction("Detail", new { id });
        }

        // ── SỬA HỒ SƠ CHUYÊN MÔN ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHoSo(int bacSiId, string hocVi, string hocHam,
            string soCCHN, string phamViHanhNghe, string ngayCapCCHN, string ngayHetHanCCHN,
            string kinhNghiem, int namKinhNghiem, string truongDaoTao,
            string chuyenMonSau, string ngonNgu, string giaiThuong)
        {
            var hoSo = _context.HoSoBacSis.FirstOrDefault(h => h.BacSiId == bacSiId);
            if (hoSo == null)
            {
                hoSo = new HoSoBacSi { BacSiId = bacSiId };
                _context.HoSoBacSis.Add(hoSo);
            }

            hoSo.HocVi = hocVi;
            hoSo.HocHam = hocHam;
            hoSo.SoCCHN = soCCHN;
            hoSo.PhamViHanhNghe = phamViHanhNghe;
            hoSo.NgayCapCCHN = string.IsNullOrEmpty(ngayCapCCHN) ? null
                                  : DateTime.TryParse(ngayCapCCHN, out var d1) ? d1 : null;
            hoSo.NgayHetHanCCHN = string.IsNullOrEmpty(ngayHetHanCCHN) ? null
                                  : DateTime.TryParse(ngayHetHanCCHN, out var d2) ? d2 : null;
            hoSo.KinhNghiem = kinhNghiem;
            hoSo.NamKinhNghiem = namKinhNghiem;
            hoSo.TruongDaoTao = truongDaoTao;
            hoSo.ChuyenMonSau = chuyenMonSau;
            hoSo.NgonNgu = ngonNgu;
            hoSo.GiaiThuong = giaiThuong;
            hoSo.CapNhatLuc = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật hồ sơ chuyên môn.";
            return RedirectToAction("Detail", new { id = bacSiId });
        }

        // ── DUYỆT YÊU CẦU CẬP NHẬT HỒ SƠ ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetYeuCau(int id, bool chapNhan, string lyDoTuChoi)
        {
            var yc = _context.YeuCauCapNhats
                .Include(y => y.BacSi)
                .FirstOrDefault(y => y.Id == id);
            if (yc == null) return NotFound();

            string adminName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin";

            if (chapNhan)
            {
                var hoSo = _context.HoSoBacSis.FirstOrDefault(h => h.BacSiId == yc.BacSiId);
                if (hoSo == null)
                {
                    hoSo = new HoSoBacSi { BacSiId = yc.BacSiId };
                    _context.HoSoBacSis.Add(hoSo);
                    await _context.SaveChangesAsync();
                }

                ApplyYeuCauToHoSo(hoSo, yc);
                hoSo.CapNhatLuc = DateTime.Now;
                yc.TrangThai = 1;
                TempData["Success"] = "Đã duyệt và áp dụng thay đổi.";
            }
            else
            {
                if (string.IsNullOrEmpty(lyDoTuChoi))
                {
                    TempData["Error"] = "Vui lòng nhập lý do từ chối.";
                    return RedirectToAction("Detail", new { id = yc.BacSiId });
                }
                yc.TrangThai = 2;
                yc.LyDoTuChoi = lyDoTuChoi;
                TempData["Error"] = "Đã từ chối yêu cầu cập nhật.";
            }

            yc.DuyetLuc = DateTime.Now;
            yc.DuyetBoi = adminName;
            await _context.SaveChangesAsync();

            return RedirectToAction("Detail", new { id = yc.BacSiId });
        }

        // ── DANH SÁCH YÊU CẦU CHỜ DUYỆT ──
        public IActionResult YeuCauCapNhat()
        {
            var yeuCaus = _context.YeuCauCapNhats
                .Include(y => y.BacSi)
                .Where(y => y.TrangThai == 0)
                .OrderByDescending(y => y.TaoLuc)
                .ToList();
            return View(yeuCaus);
        }

        // ── VÔ HIỆU HÓA BÁC SĨ (không xóa HoSo) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var bacSi = _context.Doctors
                .Include(b => b.HoSoBacSi)
                .FirstOrDefault(b => b.Id == id);
            if (bacSi == null) return NotFound();

            // ✅ KHÔNG xóa HoSoBacSi — giữ lại để có thể kích hoạt lại sau
            // Chỉ set trạng thái
            bacSi.TrangThai = 3; // Đã nghỉ việc

            var user = _context.Users.FirstOrDefault(u => u.Id == bacSi.UserId);
            if (user != null) user.IsActive = false;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã vô hiệu hóa tài khoản BS. " + bacSi.FullName;
            return RedirectToAction("Index");
        }

        // ── RESET MẬT KHẨU ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetMatKhau(int id)
        {
            var bacSi = _context.Doctors.FirstOrDefault(b => b.Id == id);
            if (bacSi == null) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == bacSi.UserId);
            if (user == null)
            {
                TempData["Error"] = "Bác sĩ này chưa có tài khoản.";
                return RedirectToAction("Detail", new { id });
            }

            string newPassword = PasswordHelper.GenerateRandom();
            string oldHash = user.Password;

            // ✅ Hash trước khi lưu
            user.Password = PasswordHelper.Hash(newPassword);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendResetPasswordAsync(
                    user.Email, bacSi.FullName ?? "Bác sĩ", newPassword);
                TempData["Success"] = "Đã reset mật khẩu và gửi email cho BS. " + bacSi.FullName;
            }
            catch
            {
                // ✅ Rollback nếu email thất bại
                user.Password = oldHash;
                await _context.SaveChangesAsync();
                TempData["Error"] = "Không thể gửi email reset. Mật khẩu chưa thay đổi. Vui lòng thử lại.";
            }

            TempData["ActiveTab"] = "taikhoan";
            return RedirectToAction("Detail", new { id });
        }

        // ── HELPER DÙNG CHUNG: áp dụng giá trị yêu cầu vào HoSo ──
        // ✅ Tách ra method riêng, dùng cả ở BacSiController lẫn YeuCauCapNhatController
        private static void ApplyYeuCauToHoSo(HoSoBacSi hoSo, YeuCauCapNhat yc)
        {
            switch (yc.TenTruong)
            {
                case "HocVi": hoSo.HocVi = yc.GiaTriMoi; break;
                case "HocHam": hoSo.HocHam = yc.GiaTriMoi; break;
                case "SoCCHN": hoSo.SoCCHN = yc.GiaTriMoi; break;
                case "PhamViHanhNghe": hoSo.PhamViHanhNghe = yc.GiaTriMoi; break;
                case "KinhNghiem": hoSo.KinhNghiem = yc.GiaTriMoi; break;
                case "TruongDaoTao": hoSo.TruongDaoTao = yc.GiaTriMoi; break;
                case "ChuyenMonSau": hoSo.ChuyenMonSau = yc.GiaTriMoi; break;
                case "GiaiThuong": hoSo.GiaiThuong = yc.GiaTriMoi; break;
                case "NgonNgu": hoSo.NgonNgu = yc.GiaTriMoi; break;
                case "NamKinhNghiem":
                    if (int.TryParse(yc.GiaTriMoi, out int soNam))
                        hoSo.NamKinhNghiem = soNam;
                    break;
            }
        }
    }
}
