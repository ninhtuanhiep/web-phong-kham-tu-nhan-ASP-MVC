using Microsoft.EntityFrameworkCore;
using System;
using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services.Giao_diện;

namespace web_phong_kham_tu_nhan.Services.Triển_khai
{
    public class DoctorService : IDoctorServices
    {
        private readonly ApplicationDbContext _context;
        public DoctorService(ApplicationDbContext context)
        {
            _context = context;
        }

     
        public List<BacSi> GetAll()
        {
            return _context.Doctors
                .Include(x => x.ChuyenKhoa)
                .Include(x => x.LichHens)
                .ThenInclude(x => x.BenhNhan)
                .ToList();
        }

        public BacSi GetById(int id)
        {
            return _context.Doctors.Find(id);
        }

        public void Add(BacSi model)
        {
            _context.Doctors.Add(model);
            _context.SaveChanges();
        }

        public void Update(BacSi model)
        {
            _context.Doctors.Update(model);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var data = _context.Doctors.Find(id);

            if (data != null)
            {
                _context.Doctors.Remove(data);
                _context.SaveChanges();
            }
        }
        public List<ChuyenKhoa> GetChuyenKhoas()
        {
            return _context.Specialties.ToList();
        }
    }
}
