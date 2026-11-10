using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.DoctorItem;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface IDoctorService
    {
        Task<ApiResponse<List<DoctorItem>>?> GetDoctorsAsync(int facilityId, int specialityId);

        // One doctor by id (GetDoctorList with doctorId) — the record includes the speciality
        Task<ApiResponse<List<DoctorItem>>?> GetDoctorAsync(int facilityId, int doctorId);
    }
}
