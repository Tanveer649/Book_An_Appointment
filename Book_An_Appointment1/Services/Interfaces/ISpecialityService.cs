using Book_An_Appointment1.Models.Speciality;
using Book_An_Appointment1.Models.Common;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface ISpecialityService
    {
        Task<ApiResponse<List<SpecialityItem>>?> GetSpecialitiesAsync(int facilityId);
    }
}
