using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services;

namespace web_phong_kham_tu_nhan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LichLamViecBacSiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailSvc;
        private readonly ThongBaoService _thongBaoSvc;

        public LichLamViecBacSiController(ApplicationDbContext context, EmailService emailSvc, ThongBaoService thongBaoSvc)
        {
            _context  = context;
            _emailSvc = emailSvc;
            _thongBaoSvc = thongBaoSvc;
        }

        // ── TRANG CHÍNH ────────────────────────────────────────────────────────
        public IActionResult Index(int? bacSiId, string view, string tuan)
        {
            DateTime startOfWeek;
            if (!DateTime.TryParse(tuan, out startOfWeek))
            {
                int diff = (7 + (int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                startOfWeek = DateTime.Today.AddDays(-diff);
            }
            DateTime endOfWeek = startOfWeek.AddDays(6);

            var bacSis = _context.Doctors
                .Where(d => d.TrangThai != 3).OrderBy(d => d.FullName).ToList();

            ViewBag.BacSis      = new SelectList(bacSis, "Id", "FullName", bacSiId);
            ViewBag.BacSiId     = bacSiId;
            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek   = endOfWeek;
            ViewBag.ViewMode    = view ?? "tuan";

            IQueryable<LichLamViecBacSi> query = _context.LichLamViecBacSis.Include(l => l.BacSi);
            if (bacSiId.HasValue) query = query.Where(l => l.BacSiId == bacSiId.Value);

            var lichTuan = query
                .Where(l => l.Ngay >= startOfWeek && l.Ngay <= endOfWeek)
                .OrderBy(l => l.Ngay).ThenBy(l => l.BacSiId).ToList();

            ViewBag.DonChoDuyet = _context.LichLamViecBacSis.Count(l => l.TrangThai == 3);
            return View(lichTuan);
        }

        // ── TẠO LỊCH (Admin trực tiếp → duyệt luôn) ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int bacSiId, string ngayBatDau, string ngayKetThuc,
                                    string ca, int soBenhNhanToiDa, string ghiChu)
        {
            DateTime start, end;
            if (!DateTime.TryParse(ngayBatDau, out start) || !DateTime.TryParse(ngayKetThuc, out end))
            { TempData["Error"] = "Ngày không hợp lệ."; return RedirectToAction("Index"); }
            if (end < start)
            { TempData["Error"] = "Ngày kết thúc phải sau ngày bắt đầu."; return RedirectToAction("Index"); }

            int soNgayTao = 0;
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                bool existed = _context.LichLamViecBacSis.Any(l =>
                    l.BacSiId == bacSiId && l.Ngay.Date == d.Date && l.CaLam == ca);
                if (existed) continue;

                _context.LichLamViecBacSis.Add(new LichLamViecBacSi
                {
                    BacSiId = bacSiId, Ngay = d, CaLam = ca, TrangThai = 0,
                    SoBenhNhanToiDa = soBenhNhanToiDa > 0 ? soBenhNhanToiDa : 20,
                    GhiChu = ghiChu, GhiChuGoc = ghiChu,
                    ThoiGianTao = DateTime.Now, TaoLichBoi = "Admin"
                });
                soNgayTao++;
            }
            _context.SaveChanges();
            TempData["Success"] = $"Đã tạo {soNgayTao} lịch làm việc.";
            return RedirectToAction("Index", new { tuan = ngayBatDau });
        }
        // ── DANH SÁCH ĐĂNG KÝ LỊCH CHỜ DUYỆT (TrangThai=4) ──────────────────
        public IActionResult DanhSachDangKy()
        {
            var list = _context.LichLamViecBacSis
                .Include(l => l.BacSi)
                .Where(l => l.TrangThai == 4)
                .OrderBy(l => l.Ngay)
                .ToList();
            ViewBag.DonChoDuyet = _context.LichLamViecBacSis.Count(l => l.TrangThai == 3);
            ViewBag.DangKyChoDuyet = list.Count;
            return View(list);
        }
        // ── DUYỆT / TỪ CHỐI ĐĂNG KÝ LỊCH ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DuyetDangKy(int id, bool chapNhan, string? ghiChuAdmin)
        {
            var lich = _context.LichLamViecBacSis
                .Include(l => l.BacSi)
                .FirstOrDefault(l => l.Id == id);
            if (lich == null) return NotFound();

            if (chapNhan)
            {
                lich.TrangThai = 0;   // Duyệt → Làm việc
                if (!string.IsNullOrEmpty(ghiChuAdmin))
                    lich.GhiChu = ghiChuAdmin;
            }
            else
            {
                // Từ chối → xóa lịch đăng ký
                _context.LichLamViecBacSis.Remove(lich);
            }

            _context.SaveChanges();

            // Thông báo bác sĩ
            if (lich.BacSi != null)
            {
                var bacSiUser = _context.Users.FirstOrDefault(u => u.Id == lich.BacSi.UserId);
                if (bacSiUser != null)
                {
                    _thongBaoSvc.AdminDuyetLich(
                        bacSiUser.Id,
                        lich.Ngay.ToString("dd/MM/yyyy"),
                        chapNhan,
                        ghiChuAdmin,
                        lich.Id
                    );
                    _thongBaoSvc.SaveChanges();
                }
            }

            TempData["Success"] = chapNhan ? "Đã duyệt lịch đăng ký." : "Đã từ chối và xóa lịch đăng ký.";
            return RedirectToAction("DanhSachDangKy");
        }

        // ── DANH SÁCH ĐƠN XIN NGHỈ ───────────────────────────────────────────
        public IActionResult DonXinNghi()
        {
            var dons = _context.LichLamViecBacSis
                .Include(l => l.BacSi)
                .Where(l => l.TrangThai == 3)
                .OrderBy(l => l.Ngay)
                .ToList();

            ViewBag.DonChoDuyet = dons.Count;
            return View(dons);
        }

        // ── CHI TIẾT ĐƠN XIN NGHỈ + SỐ LỊCH HẸN TRONG NGÀY ─────────────────
        // Admin gọi action này để xem đầy đủ trước khi quyết định
        public IActionResult ChiTietDon(int id)
        {
            var lich = _context.LichLamViecBacSis
                .Include(l => l.BacSi)
                .FirstOrDefault(l => l.Id == id);
            if (lich == null) return NotFound();

            // Đếm và lấy danh sách lịch hẹn của bác sĩ trong ngày đó
            var lichHenTrongNgay = _context.Appointments
                .Include(a => a.BenhNhan)
                .Where(a => a.BacSiId == lich.BacSiId
                         && a.AppointmentDate.Date == lich.Ngay.Date
                         && (a.TrangThai == 0 || a.TrangThai == 1))
                .OrderBy(a => a.TimeSlot)
                .ToList();

            ViewBag.LichHenTrongNgay   = lichHenTrongNgay;
            ViewBag.SoLichHenTrongNgay = lichHenTrongNgay.Count;
            ViewBag.IsDotXuat          = (lich.GhiChu ?? "").Contains("[Nghỉ đột xuất]")
                                      || (lich.LyDoXinNghi ?? "").Contains("đột xuất");
            return View(lich);
        }

        // ── DUYỆT / TỪ CHỐI ĐƠN XIN NGHỈ ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetNghi(int id, bool chaphuan, bool dotXuat = false)
        {
            var lich = _context.LichLamViecBacSis
                .Include(l => l.BacSi)
                .FirstOrDefault(l => l.Id == id);
            if (lich == null) return NotFound();

            string ngayStr  = lich.Ngay.ToString("dd/MM/yyyy");
            string tenBacSi = lich.BacSi?.FullName ?? "Bác sĩ";

            if (chaphuan)
            {
                lich.TrangThai = dotXuat ? 5 : 1; // 5=Nghỉ đột xuất, 1=Nghỉ phép

                if (dotXuat)
                {
                    // Lấy tất cả lịch hẹn bị ảnh hưởng
                    var lichHenBiAnh = _context.Appointments
                        .Include(a => a.BenhNhan)
                        .Where(a => a.BacSiId    == lich.BacSiId
                                 && a.AppointmentDate.Date == lich.Ngay.Date
                                 && (a.TrangThai == 0 || a.TrangThai == 1))
                        .ToList();

                    foreach (var lh in lichHenBiAnh)
                    {
                        // Cập nhật trạng thái lịch hẹn → 4 (bị hủy do bác sĩ nghỉ)
                        lh.TrangThai = 4;

                        if (lh.BenhNhan == null) continue;

                        // Tìm email bệnh nhân
                        var bnUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Id == lh.BenhNhan.UserId);
                        if (bnUser == null) continue;

                        string tenBN  = lh.BenhNhan.FullName ?? bnUser.FullName ?? "Bệnh nhân";
                        string emailBN = lh.BenhNhan.Email ?? bnUser.Email ?? "";
                        string gioKham = lh.TimeSlot ?? "Theo lịch hẹn";

                        // 1. Gửi EMAIL cho bệnh nhân
                        if (!string.IsNullOrEmpty(emailBN))
                        {
                            try
                            {
                                await _emailSvc.SendBacSiNghiDotXuatAsync(
                                    emailBN, tenBN, tenBacSi, ngayStr, gioKham);
                            }
                            catch (Exception ex)
                            {
                                // Ghi log lỗi nhưng không dừng process
                                Console.WriteLine($"[Email Error] {emailBN}: {ex.Message}");
                            }
                        }

                        // 2. Tạo thông báo in-app
                        _context.ThongBaos.Add(new ThongBao
                        {
                            NguoiNhanId  = bnUser.Id,
                            LoaiThongBao = "BacSiNghi",
                            TieuDe       = $"⚠️ Lịch khám ngày {ngayStr} bị thay đổi",
                            NoiDung      = $"Bác sĩ {tenBacSi} có việc đột xuất và không thể khám vào ngày {ngayStr}. " +
                                           $"Lịch hẹn {gioKham} của bạn đã được chuyển sang trạng thái chờ sắp xếp lại. " +
                                           $"Phòng khám sẽ liên hệ để đặt lại lịch sớm nhất.",
                            LichHenId    = lh.Id,
                            ThoiGianTao  = DateTime.Now
                        });
                    }
                }
            }
            else
            {
                // Từ chối → trả lịch về trạng thái làm việc
                lich.TrangThai   = 0;
                lich.GhiChu      = lich.GhiChuGoc;
                lich.LyDoXinNghi = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = chaphuan
                ? (dotXuat
                    ? $"Đã duyệt nghỉ đột xuất. Đã gửi email thông báo đến bệnh nhân có lịch hẹn ngày {ngayStr}."
                    : "Đã duyệt đơn xin nghỉ phép.")
                : "Đã từ chối đơn xin nghỉ, lịch giữ nguyên.";

            return RedirectToAction("DonXinNghi");
        }

        // ── XÓA LỊCH ─────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var lich = _context.LichLamViecBacSis.Find(id);
            if (lich == null) return NotFound();
            _context.LichLamViecBacSis.Remove(lich);
            _context.SaveChanges();
            TempData["Success"] = "Đã xóa lịch làm việc.";
            return RedirectToAction("Index");
        }
    }
}
