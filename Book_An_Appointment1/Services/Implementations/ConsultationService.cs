using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Consultation;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;

namespace Book_An_Appointment1.Services.Implementations
{
    public class ConsultationService : BaseApiClient, IConsultationService
    {
        public ConsultationService(
            IHttpClientFactory clientFactory,
            ILogger<ConsultationService> logger,
            ApiUrlBuilder apiUrlBuilder)
            : base(clientFactory, logger, apiUrlBuilder)
        {
        }

        public async Task<List<ConsultationItem>?> GetConsultationFeeAsync(
            int facilityId,
            int doctorId)
        {

            var url = ApiUrlBuilder.BuildUrl(ApiRoutes.GetConsultationCharges, "GetConsultationParams",
                new Dictionary<string, string> 
                {
                    { "facilityCode", facilityId.ToString() },
                    { "doctorId", doctorId.ToString() }
                });

            var response = await GetAsync<ApiResponse<List<ConsultationItem>>>(url);

            return response?.Data;
        }
    }
}