using web_phong_kham_tu_nhan.Data;
using web_phong_kham_tu_nhan.Models.Entities;
using web_phong_kham_tu_nhan.Services.Giao_diện;
namespace web_phong_kham_tu_nhan.Services.Triển_khai
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;

        public PatientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<BenhNhan> GetAll()
        {
            return _context.Patients.ToList();
        }

        public BenhNhan GetById(int id)
        {
            return _context.Patients.Find(id);
        }

        public void Add(BenhNhan model)
        {
            _context.Patients.Add(model);
            _context.SaveChanges();
        }

        public void Update(BenhNhan model)
        {
            _context.Patients.Update(model);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var data = _context.Patients.Find(id);
            if (data != null)
            {
                _context.Patients.Remove(data);
                _context.SaveChanges();
            }
        }
    }
}
