
using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.DoctorItem;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;

namespace Book_An_Appointment1.Services.Implementations
{
    public class DoctorService : BaseApiClient, IDoctorService
    {
        private readonly IConfiguration _configuration;
        public DoctorService(IHttpClientFactory clientFactory, 
            ILogger<DoctorService> logger, 
            ApiUrlBuilder apiUrlBuilder,
            IConfiguration configuration) : base(clientFactory, logger, apiUrlBuilder) 
        { 
            _configuration = configuration; 
        }

        public async Task<ApiResponse<List<DoctorItem>>?> GetDoctorsAsync(int facilityId, int specialityId)
        {
            var url = ApiUrlBuilder.BuildUrl(ApiRoutes.GetDoctors, "GetDoctorsParams",
                    new Dictionary<string, string>
                    {
                        { "facilityCode", facilityId.ToString() },
                        { "specializationId", specialityId.ToString() }                   
                    });
            return await GetAsync<ApiResponse<List<DoctorItem>>>(url);
        }

        public async Task<ApiResponse<List<DoctorItem>>?> GetDoctorAsync(int facilityId, int doctorId)
        {
            // Config GetDoctorsParams already has doctorId=0 → replaced in place (not sent twice).
            // specializationId 0 = any speciality; the doctor record returned carries its own specialisationId.
            var url = ApiUrlBuilder.BuildUrl(ApiRoutes.GetDoctors, "GetDoctorsParams",
                    new Dictionary<string, string>
                    {
                        { "facilityCode", facilityId.ToString() },
                        { "specializationId", "0" }
                    });
            url = System.Text.RegularExpressions.Regex.Replace(url, @"([?&])doctorId=[^&]*", "$1doctorId=" + doctorId);
            return await GetAsync<ApiResponse<List<DoctorItem>>>(url);
        }
    }
}
