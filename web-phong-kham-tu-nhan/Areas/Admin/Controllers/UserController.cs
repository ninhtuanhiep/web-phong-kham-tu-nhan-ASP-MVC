using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Helpers;
using web_phong_kham_tu_nhan.Models.Entities;
using X.PagedList.Extensions;

namespace web_phong_kham_tu_nhan.Area.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── DANH SÁCH ──
        public IActionResult Index(string? role, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            // ✅ Tính thống kê trên DB, không load toàn bộ về RAM
            ViewBag.AllUser = _context.Users.Count();
            ViewBag.Admin = _context.Users.Count(p => p.Role == "Admin");
            ViewBag.BenhNhan = _context.Users.Count(p => p.Role == "Bệnh nhân");
            ViewBag.BacSi = _context.Users.Count(p => p.Role == "Bác sĩ");

            var query = _context.Users.AsQueryable();
            if (role != null)
                query = query.Where(p => p.Role == role);

            var pagedUsers = query
                .OrderByDescending(x => x.CreateAt)
                .ToPagedList(pageNumber, pageSize);

            return View(pagedUsers);
        }

        // ── BẬT/TẮT TRẠNG THÁI ──
        // ✅ Đổi sang POST để tránh CSRF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _context.SaveChanges();
                TempData["Success"] = user.IsActive
                    ? "Đã kích hoạt tài khoản " + user.Email
                    : "Đã khóa tài khoản " + user.Email;
            }
            else
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
            }
            return RedirectToAction("Index");
        }

        // ── TẠO MỚI ──
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User model)
        {
            // Kiểm tra email trùng
            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // ✅ Hash mật khẩu trước khi lưu
                if (!string.IsNullOrEmpty(model.Password))
                    model.Password = PasswordHelper.Hash(model.Password);

                model.CreateAt = DateTime.Now;
                _context.Users.Add(model);
                _context.SaveChanges();
                TempData["Success"] = "Thêm người dùng thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // ── SỬA ──
        public IActionResult Edit(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            // ✅ Không trả password ra view — clear trước khi render
            user.Password = null;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.Find(model.Id);
                if (user == null) return NotFound();

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Role = model.Role;
                user.IsActive = model.IsActive;
                user.PhoneNumber = model.PhoneNumber;

                // ✅ Chỉ cập nhật password khi admin nhập mới, và phải hash
                if (!string.IsNullOrEmpty(model.Password))
                    user.Password = PasswordHelper.Hash(model.Password);
                // Nếu để trống → giữ nguyên hash cũ

                _context.SaveChanges();
                TempData["Success"] = "Cập nhật người dùng thành công.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // ── CHI TIẾT ──
        public IActionResult Detail(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // ── XÓA ──
        // ✅ Đổi sang POST + AntiForgery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa người dùng thành công.";
            }
            return RedirectToAction("Index");
        }
    }
}
