using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Consultation;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;

namespace Book_An_Appointment1.Services.Implementations
{
    public class ConsultationService :
        BaseApiClient,
        IConsultationService
    {
        public ConsultationService(
            IHttpClientFactory clientFactory,
            ILogger<ConsultationService> logger)
            : base(clientFactory, logger)
        {
        }

        public async Task<List<ConsultationResponse>?> GetConsultationFeeAsync(
            int facilityId,
            int doctorId)
        {
            var url =
                $"/api/VCP/GetConsultationCharges" +
                $"?facilityCode={facilityId}" +
                $"&registrationNo=0" +
                $"&appointmentId=0" +
                $"&doctorId={doctorId}" +
                $"&appointmentType=RC" +
                $"&doctorRegistrationNo=0" +
                $"&GuestPateintId=0";

            var response = await GetAsync<ApiResponse<List<ConsultationResponse>>>(url);

            return response?.Data;

            //return response?
            //    .Data?
            //    .FirstOrDefault(x => x.ServiceType == "RF")
            //    ?.Price;
        }
    }
}