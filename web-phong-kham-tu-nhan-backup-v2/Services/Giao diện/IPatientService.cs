using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Services.Giao_diện
{
    public interface IPatientService
    {
        List<BenhNhan> GetAll();
        BenhNhan GetById(int id);
        void Add(BenhNhan model);
        void Update(BenhNhan model);
        void Delete(int id);

    }
}
