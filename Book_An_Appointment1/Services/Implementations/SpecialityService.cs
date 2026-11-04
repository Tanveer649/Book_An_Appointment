
using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Speciality;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;
using Microsoft.Extensions.Caching.Memory;

namespace Book_An_Appointment1.Services.Implementations;

public class SpecialityService : BaseApiClient, ISpecialityService
{
    private readonly IMemoryCache _cache;
    public SpecialityService(IHttpClientFactory clientFactory,
        ILogger<SpecialityService> logger,
        ApiUrlBuilder apiUrlBuilder,
        IMemoryCache cache) : base(clientFactory, logger, apiUrlBuilder)
    {
        _cache = cache;
    }
    public async Task<ApiResponse<List<SpecialityItem>>?> GetSpecialitiesAsync(int facilityId)
    {
        // Same lifetime as the facility list — specialities change rarely
        var cacheKey = $"specialities_{facilityId}";
        if (_cache.TryGetValue(cacheKey, out ApiResponse<List<SpecialityItem>>? cached))
            return cached;

        var url = ApiUrlBuilder.BuildUrl(ApiRoutes.GetSpecialities, "GetSpecialitiesParams",
                    new Dictionary<string, string>
                    {
                        { "facilityId", facilityId.ToString() }
                    });

        var response = await GetAsync<ApiResponse<List<SpecialityItem>>>(url);

        if (response?.Data?.Count > 0)
            _cache.Set(cacheKey, response, TimeSpan.FromHours(6));

        return response;
    }
}
