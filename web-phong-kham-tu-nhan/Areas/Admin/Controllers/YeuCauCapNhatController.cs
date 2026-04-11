using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class YeuCauCapNhatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public YeuCauCapNhatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── DANH SÁCH TẤT CẢ YÊU CẦU ──
        public IActionResult Index(string trangThai, string search)
        {
            var query = _context.YeuCauCapNhats
                .Include(y => y.BacSi)
                .AsQueryable();

            // Filter theo trạng thái
            if (!string.IsNullOrEmpty(trangThai) && trangThai != "all")
            {
                if (int.TryParse(trangThai, out int tt))
                    query = query.Where(y => y.TrangThai == tt);
            }

            // Tìm kiếm theo tên bác sĩ
            if (!string.IsNullOrEmpty(search))
                query = query.Where(y => y.BacSi.FullName.Contains(search));

            var list = query.OrderByDescending(y => y.TaoLuc).ToList();

            ViewBag.TrangThai = trangThai ?? "0";
            ViewBag.Search = search;
            ViewBag.TongChoduyet = _context.YeuCauCapNhats.Count(y => y.TrangThai == 0);
            ViewBag.TongDaDuyet = _context.YeuCauCapNhats.Count(y => y.TrangThai == 1);
            ViewBag.TongTuchoi = _context.YeuCauCapNhats.Count(y => y.TrangThai == 2);

            return View(list);
        }

        // ── DUYỆT YÊU CẦU ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duyet(int id, bool chapNhan, string lyDoTuChoi)
        {
            var yc = _context.YeuCauCapNhats
                .Include(y => y.BacSi)
                .FirstOrDefault(y => y.Id == id);

            if (yc == null) return NotFound();

            string adminName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin";

            if (chapNhan)
            {
                // Áp dụng thay đổi vào HoSoBacSi
                var hoSo = _context.HoSoBacSis.FirstOrDefault(h => h.BacSiId == yc.BacSiId);
                if (hoSo == null)
                {
                    hoSo = new HoSoBacSi { BacSiId = yc.BacSiId, CapNhatLuc = DateTime.Now };
                    _context.HoSoBacSis.Add(hoSo);
                    await _context.SaveChangesAsync();
                }

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
                hoSo.CapNhatLuc = DateTime.Now;
                yc.TrangThai = 1;
                TempData["Success"] = "Đã duyệt và áp dụng thay đổi cho BS. " + yc.BacSi.FullName;
            }
            else
            {
                if (string.IsNullOrEmpty(lyDoTuChoi))
                {
                    TempData["Error"] = "Vui lòng nhập lý do từ chối.";
                    return RedirectToAction("Index");
                }
                yc.TrangThai = 2;
                yc.LyDoTuChoi = lyDoTuChoi;
                TempData["Error"] = "Đã từ chối yêu cầu.";
            }

            yc.DuyetLuc = DateTime.Now;
            yc.DuyetBoi = adminName;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { trangThai = "0" });
        }

        // ── DUYỆT NHIỀU YÊU CẦU CÙNG LÚC ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetNhieu(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                TempData["Error"] = "Chưa chọn yêu cầu nào.";
                return RedirectToAction("Index");
            }

            string adminName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Admin";
            int count = 0;

            foreach (int id in ids)
            {
                var yc = _context.YeuCauCapNhats.FirstOrDefault(y => y.Id == id && y.TrangThai == 0);
                if (yc == null) continue;

                var hoSo = _context.HoSoBacSis.FirstOrDefault(h => h.BacSiId == yc.BacSiId);
                if (hoSo == null)
                {
                    hoSo = new HoSoBacSi { BacSiId = yc.BacSiId, CapNhatLuc = DateTime.Now };
                    _context.HoSoBacSis.Add(hoSo);
                    await _context.SaveChangesAsync();
                }

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
                hoSo.CapNhatLuc = DateTime.Now;
                yc.TrangThai = 1;
                yc.DuyetLuc = DateTime.Now;
                yc.DuyetBoi = adminName;
                count++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã duyệt " + count + " yêu cầu.";
            return RedirectToAction("Index", new { trangThai = "0" });
        }

        // ── XÓA YÊU CẦU ĐÃ XỬ LÝ ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Xoa(int id)
        {
            var yc = _context.YeuCauCapNhats.Find(id);
            if (yc == null) return NotFound();
            if (yc.TrangThai == 0)
            {
                TempData["Error"] = "Không thể xóa yêu cầu đang chờ duyệt.";
                return RedirectToAction("Index");
            }
            _context.YeuCauCapNhats.Remove(yc);
            _context.SaveChanges();
            TempData["Success"] = "Đã xóa yêu cầu.";
            return RedirectToAction("Index");
        }
    }
}
