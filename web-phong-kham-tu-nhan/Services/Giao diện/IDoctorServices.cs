using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Services.Giao_diện
{
    public interface IDoctorServices
    {
        List<BacSi> GetAll();
        BacSi GetById(int id);
        void Add(BacSi doctor);
        void Update(BacSi doctor);
        void Delete(int id);

        List<ChuyenKhoa> GetChuyenKhoas();
    }
}
