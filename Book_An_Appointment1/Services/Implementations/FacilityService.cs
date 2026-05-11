using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Facility;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;
using Newtonsoft.Json.Linq;

namespace Book_An_Appointment1.Services
{
    public class FacilityService : BaseApiClient, IFacilityService
    {
        private readonly IConfiguration _configuration;

        public FacilityService(IHttpClientFactory clientFactory, ILogger<FacilityService> logger, IConfiguration configuration) : base(clientFactory, logger)
        {
            _configuration = configuration;
        }
        public async Task<ApiResponse<List<FacilityItem>>?> GetFacilitiesAsync()
        {
            var facilityCode = _configuration["ApiSettings:FacilityCode"];

            var hospitalLocationId = _configuration["ApiSettings:HospitalLocationId"];

            var url = $"{ApiRoutes.GetFacilities}" + $"?facilityCode={facilityCode}" + $"&hospitalLocationId={hospitalLocationId}";


            var response = await GetAsync<ApiResponse<List<FacilityItem>>>(url);
           
            return response;
        }
    }
}
