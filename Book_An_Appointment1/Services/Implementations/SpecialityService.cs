using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Speciality;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;

namespace Book_An_Appointment1.Services.Implementations;

public class SpecialityService : BaseApiClient, ISpecialityService
{
    private readonly IConfiguration _configuration;
    public SpecialityService(IHttpClientFactory clientFactory, ILogger<SpecialityService> logger, IConfiguration configuration) : base(clientFactory, logger)
    {
        _configuration = configuration;
    }
    public async Task<ApiResponse<List<SpecialityItem>>?> GetSpecialitiesAsync(int facilityId)
    {
        var hospitalLocationId = _configuration["ApiSettings:HospitalLocationId"];
        var appointmentTypeId = _configuration["ApiSettings:AppointmentTypeId"];

        var url = $"{ApiRoutes.GetSpecialities}" + 
                  $"?facilityId={facilityId}" + 
                  $"&hospitalLocationId={hospitalLocationId}" + 
                  $"&appointmentType={appointmentTypeId}"; 

        return await GetAsync<ApiResponse<List<SpecialityItem>>>(url);
    }
}