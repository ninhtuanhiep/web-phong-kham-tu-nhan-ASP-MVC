using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Services.Giao_diện
{
    public interface ISpecialtyService
    {
        List<ChuyenKhoa> GetAll();
        ChuyenKhoa GetById(int id);
        void Add(ChuyenKhoa specialty);
        void Update(ChuyenKhoa specialty);
        void Delete(int id);
    }
}
