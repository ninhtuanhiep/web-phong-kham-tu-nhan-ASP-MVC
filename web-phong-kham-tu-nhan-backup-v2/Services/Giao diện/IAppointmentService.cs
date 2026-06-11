using web_phong_kham_tu_nhan.Models.Entities;

namespace web_phong_kham_tu_nhan.Services.Giao_diện
{
    public interface IAppointmentService
    {
        List<LichHen> GetAll();
        LichHen GetById(int id);
        void Add(LichHen lichHen);
        void Update(LichHen lichHen);
        void Delete(int id);
    }
}
