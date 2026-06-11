using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services.Giao_diện;
using Microsoft.EntityFrameworkCore;

namespace web_phong_kham_tu_nhan.Services.Triển_khai
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;

        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<LichHen> GetAll()
        {
            return _context.Appointments
                .Include(x => x.BenhNhan)
                .Include(x => x.BacSi)
                .Include(x => x.ChuyenKhoa)
                .ToList();
        }

        public LichHen GetById(int id)
        {
            return _context.Appointments
                .Include(x => x.BenhNhan)
                .Include(x => x.BacSi)
                .Include(x => x.ChuyenKhoa)
                .FirstOrDefault(x => x.Id == id);
        }

        public void Add(LichHen lichHen)
        {
            _context.Appointments.Add(lichHen);
            _context.SaveChanges();
        }

        //public void Update(LichHen lichHen)
        //{
        //    _context.Appointments.Update(lichHen);
        //    _context.SaveChanges();
        //}

        public void Update(LichHen model)
        {
            var existing = _context.Appointments.FirstOrDefault(x => x.Id == model.Id);

            if (existing != null)
            {
                existing.BenhNhanId = model.BenhNhanId;
                existing.ChuyenKhoaId = model.ChuyenKhoaId;
                existing.BacSiId = model.BacSiId;
                existing.AppointmentDate = model.AppointmentDate;
                existing.TimeSlot = model.TimeSlot;
                existing.TrangThai = model.TrangThai;

                _context.SaveChanges();
            }
        }
        public void Delete(int id)
        {
            var lichHen = _context.Appointments.Find(id);

            if (lichHen != null)
            {
                _context.Appointments.Remove(lichHen);
                _context.SaveChanges();
            }
        }
    }
}
