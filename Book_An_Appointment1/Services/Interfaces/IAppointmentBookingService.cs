using Book_An_Appointment1.Models.Appointment;
using Book_An_Appointment1.Models.Common;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface IAppointmentBookingService
    {
        Task<ApiResponse<CreateAppointmentResponseData>?> CreateAppointmentAsync(CreateAppointmentRequest request);
        Task<ApiResponse<List<AppointmentDetailItem>>?> GetAppointmentByIdAsync(string appointmentId);
    }
}
