using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services.Giao_diện;
using Microsoft.EntityFrameworkCore;

namespace web_phong_kham_tu_nhan.Services.Triển_khai
{
    public class SpecialtyService : ISpecialtyService
    {
        private readonly ApplicationDbContext _context;

        public SpecialtyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ChuyenKhoa> GetAll()
        {
            return _context.Specialties
                .Include(x => x.BacSis)
                //.Include(x => x.BenhNhans)
                .ToList();
        }

        public ChuyenKhoa GetById(int id)
        {
            return _context.Specialties.Find(id);
        }

        public void Add(ChuyenKhoa specialty)
        {
            _context.Specialties.Add(specialty);
            _context.SaveChanges();
        }

        public void Update(ChuyenKhoa specialty)
        {
            _context.Specialties.Update(specialty);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var sp = _context.Specialties.Find(id);
            if (sp != null)
            {
                _context.Specialties.Remove(sp);
                _context.SaveChanges();
            }
        }
    }
}
