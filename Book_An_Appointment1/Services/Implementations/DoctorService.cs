
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
        public DoctorService(IHttpClientFactory clientFactory, ILogger<DoctorService> logger, IConfiguration configuration) : base(clientFactory, logger) 
        { 
            _configuration = configuration; 
        }

        public async Task<ApiResponse<List<DoctorItem>>?> GetDoctorsAsync(int facilityId, int specialityId)
        {
            var hospitalLocationId = _configuration["ApiSettings:HospitalLocationId"]; 
            var appointmentTypeId = _configuration["ApiSettings:AppointmentTypeId"]; 
            var url = $"{ApiRoutes.GetDoctors}" + 
                $"?facilityCode={facilityId}" + 
                $"&doctorId=0" + 
                $"&specializationId={specialityId}" + 
                $"&hospitalLocationId={hospitalLocationId}" + 
                $"&AppointmentTypeId={appointmentTypeId}";

            return await GetAsync<ApiResponse<List<DoctorItem>>>(url);
        }
    }
}
