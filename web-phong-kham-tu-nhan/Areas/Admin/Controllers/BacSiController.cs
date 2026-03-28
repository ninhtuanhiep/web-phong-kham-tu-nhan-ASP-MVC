using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services.Giao_diện;

namespace web_phong_kham_tu_nhan.Area.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BacSiController : Controller
    {
        private readonly IDoctorServices _service;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public BacSiController(IDoctorServices service, IWebHostEnvironment env, ApplicationDbContext context)
        {
            _service = service;
            _env = env;
            _context = context;
        }
        public IActionResult Index()
        {
            var data = _service.GetAll();
            return View(data);
        }

        public IActionResult Create()
        {
            ViewBag.ChuyenKhoas = new SelectList(_service.GetChuyenKhoas(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm cái này cho bảo mật
        public async Task<IActionResult> Create(BacSi model) // Bỏ [FromForm] đi cho nhẹ nợ
        {
            // Đặt dấu ngắt (Breakpoint) ở đây để kiểm tra model.ImageFile có dữ liệu không
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "Images", "doctors");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filename = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, filename);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                // BẮT BUỘC: Phải có dấu gạch chéo "/" ở đầu để nó là đường dẫn tuyệt đối
                model.ImageUrl = "/Images/doctors/" + filename;
            }
            else
            {
                // Nếu không có ảnh, hãy gán một ảnh mặc định để tránh lỗi 404
                model.ImageUrl = "/Images/default-avatar.png";
                System.Diagnostics.Debug.WriteLine("CẢNH BÁO: Không nhận được file ảnh!");
            }

            if (ModelState.IsValid)
            {
                _service.Add(model);
                TempData["Success"] = "Thêm bác sĩ thành công!";
                return RedirectToAction("Index");
            }

            // Nếu lỗi Validation (ví dụ thiếu Chuyên khoa), hãy in ra Console để sửa
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var err in errors) System.Diagnostics.Debug.WriteLine("LỖI: " + err.ErrorMessage);

            ViewBag.ChuyenKhoas = new SelectList(_service.GetChuyenKhoas(), "Id", "Name", model.ChuyenKhoaId);
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var data = _service.GetById(id);
            ViewBag.ChuyenKhoas = new SelectList(_service.GetChuyenKhoas(), "Id", "Name", data.ChuyenKhoaId);
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BacSi model)
        {
            if (ModelState.IsValid) {
                var existing = _service.GetById(model.Id);

                if (existing == null)
                    return NotFound();

                // 🔥 UPDATE TỪNG FIELD
                existing.FullName = model.FullName;
                existing.Email = model.Email;
                existing.DienThoai = model.DienThoai;
                existing.ChuyenKhoaId = model.ChuyenKhoaId;
                existing.tieuSu = model.tieuSu;
                existing.TrangThai = model.TrangThai;

                // 🔥 XỬ LÝ ẢNH
                if (model.ImageFile != null)
                {
                    string folder = Path.Combine(_env.WebRootPath, "Images", "doctors");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                    string path = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }

                    existing.ImageUrl = "/Images/doctors/" + fileName;
                }
               _context.SaveChanges(); // Lưu thay đổi vào database
                TempData["Success"] = "Cập nhật bác sĩ thành công!";
            return RedirectToAction("Index");
            }
            ViewBag.ChuyenKhoas = new SelectList(_service.GetChuyenKhoas(), "Id", "Name", model.ChuyenKhoaId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoTaiKhoan(int id)
        {
            var bacSi = _context.Doctors.Find(id);
            if (bacSi == null) return NotFound();

            // Kiểm tra đã có tài khoản chưa
            if (bacSi.UserId != null)
            {
                TempData["Error"] = "Bác sĩ này đã có tài khoản rồi.";
                return RedirectToAction("Index");
            }

            // Tạo username từ email hoặc tên
            string email = bacSi.Email ?? (bacSi.FullName.ToLower().Replace(" ", "") + "@phongkham.vn");
            string password = "Bacsi@123"; // Mật khẩu mặc định

            // Kiểm tra email đã tồn tại chưa
            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["Error"] = "Email " + email + " đã được dùng. Hãy cập nhật email cho bác sĩ trước.";
                return RedirectToAction("Index");
            }

            var user = new User
            {
                FullName = bacSi.FullName,
                Email = email,
                Password = password,
                Role = "Bác sĩ",
                PhoneNumber = bacSi.DienThoai,
                IsActive = true,
                CreateAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Gán UserId vào BacSi
            bacSi.UserId = user.Id;
            _context.SaveChanges();

            TempData["Success"] = "Đã tạo tài khoản cho BS. " + bacSi.FullName
                                + " | Email: " + email
                                + " | Mật khẩu: " + password;
            return RedirectToAction("Index");
        }
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            TempData["Success"] = "Xóa bác sĩ thành công!";
            return RedirectToAction("Index");
        }
    }
}
