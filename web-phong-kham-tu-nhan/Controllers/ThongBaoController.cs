using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;

namespace web_phong_kham_tu_nhan.Controllers
{
    [Authorize]
    public class ThongBaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThongBaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // GET /ThongBao – danh sách thông báo của người dùng hiện tại
        public async Task<IActionResult> Index()
        {
            int uid = GetCurrentUserId();
            var list = await _context.ThongBaos
                .Where(t => t.NguoiNhanId == uid)
                .OrderByDescending(t => t.ThoiGianTao)
                .ToListAsync();

            // Đánh dấu đã đọc tất cả
            var chuaDoc = list.Where(t => !t.DaDoc).ToList();
            chuaDoc.ForEach(t => t.DaDoc = true);
            if (chuaDoc.Any()) await _context.SaveChangesAsync();

            return View(list);
        }

        // POST /ThongBao/DocMot/{id}
        [HttpPost]
        public async Task<IActionResult> DocMot(int id)
        {
            int uid = GetCurrentUserId();
            var tb  = await _context.ThongBaos.FirstOrDefaultAsync(t => t.Id == id && t.NguoiNhanId == uid);
            if (tb != null) { tb.DaDoc = true; await _context.SaveChangesAsync(); }
            return Ok();
        }

        // GET /ThongBao/SoLuongChuaDoc – AJAX endpoint cho badge
        [HttpGet]
        public async Task<IActionResult> SoLuongChuaDoc()
        {
            int uid   = GetCurrentUserId();
            int count = await _context.ThongBaos.CountAsync(t => t.NguoiNhanId == uid && !t.DaDoc);
            return Json(new { count });
        }

        // GET /ThongBao/DanhSachMoi – AJAX lấy 5 thông báo mới nhất
        [HttpGet]
        public async Task<IActionResult> DanhSachMoi()
        {
            int uid = GetCurrentUserId();
            var list = await _context.ThongBaos
                .Where(t => t.NguoiNhanId == uid)
                .OrderByDescending(t => t.ThoiGianTao)
                .Take(5)
                .Select(t => new {
                    t.Id, t.TieuDe, t.NoiDung, t.DaDoc,
                    t.LoaiThongBao, t.LichHenId, t.LichLamViecId,
                    thoiGian = t.ThoiGianTao.ToString("dd/MM HH:mm")
                })
                .ToListAsync();
            return Json(list);
        }
    }
}
