using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Appointment;
using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;

namespace Book_An_Appointment1.Services.Implementations
{
    public class AppointmentBookingService : BaseApiClient, IAppointmentBookingService
    {
        public AppointmentBookingService(IHttpClientFactory clientFactory,
            ILogger<AppointmentBookingService> logger,
            ApiUrlBuilder apiUrlBuilder) : base(clientFactory, logger, apiUrlBuilder)
        {
        }

        public async Task<ApiResponse<CreateAppointmentResponseData>?> CreateAppointmentAsync(CreateAppointmentRequest request)
        {
            return await PostAsync<ApiResponse<CreateAppointmentResponseData>>(ApiRoutes.CreateAppointment, request);
        }

        public async Task<ApiResponse<List<AppointmentDetailItem>>?> GetAppointmentByIdAsync(string appointmentId)
        {
            var url = $"{ApiRoutes.GetAppointmentByID}?appointmentId={Uri.EscapeDataString(appointmentId)}";
            return await GetAsync<ApiResponse<List<AppointmentDetailItem>>>(url);
        }
    }
}
