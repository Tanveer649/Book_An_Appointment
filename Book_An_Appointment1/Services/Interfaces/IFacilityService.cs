using Book_An_Appointment1.Models.Facility;
using Book_An_Appointment1.Models.Common;

namespace Book_An_Appointment1.Services.Interfaces;

public interface IFacilityService
{
    Task<ApiResponse<List<FacilityItem>>?> GetFacilitiesAsync();
}
