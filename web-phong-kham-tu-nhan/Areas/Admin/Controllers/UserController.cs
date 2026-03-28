using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_phong_kham_tu_nhan.Data;
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
        public IActionResult Index(string? role,int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var users = _context.Users
                .OrderByDescending(x =>x.CreateAt)
                .ToList();

            ViewBag.AllUser = users.Count();
            ViewBag.Admin = users.Count(p => p.Role == "Admin");
            ViewBag.BenhNhan = users.Count(p => p.Role == "Bệnh nhân");
            ViewBag.BacSi = users.Count(p => p.Role == "Bác sĩ");

            if(role != null)
            {
                users = users.Where(p => p.Role == role).ToList();
            }

            var pagedUsers = users
                .OrderBy(x => x.Id)
                .ToPagedList(pageNumber, pageSize);

            return View(pagedUsers);
        }

        public IActionResult ToggleStatus(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _context.SaveChanges();
                TempData["Success"] = "Cập nhật trạng thái người dùng thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
            }
            return RedirectToAction("Index");
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(User model)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Add(model);
                _context.SaveChanges();
                TempData["Success"] = "Thêm người dùng thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.Find(model.Id);

                if (user == null)
                {
                    return NotFound();
                }

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Role = model.Role;
                user.IsActive = model.IsActive;
                user.PhoneNumber = model.PhoneNumber;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.Password = model.Password;
                }

                _context.SaveChanges();

                TempData["Success"] = "Cập nhật người dùng thành công";

                return RedirectToAction("Index");
            }

            return View(model);
        }

        public IActionResult Detail(int id)
        {
            var user = _context.Users.Find(id);
            if(user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if( user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "Xóa người dùng thành công";

            }
            return RedirectToAction("Index");
        }
    }
}
