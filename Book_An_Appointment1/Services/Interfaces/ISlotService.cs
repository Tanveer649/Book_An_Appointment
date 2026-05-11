using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Slot;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface ISlotService
    {
        Task<ApiResponse<DoctorSlotResponse>?> GetSlotsAsync(
            int facilityId,
            int doctorId,
            int hospitalLocationId,
            string fromDate,
            string toDate);
    }
}