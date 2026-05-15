using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Facility;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace Book_An_Appointment1.Services
{
    public class FacilityService : BaseApiClient, IFacilityService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "facilities_data";

        public FacilityService(
            IHttpClientFactory clientFactory,
            ILogger<FacilityService> logger,
            ApiUrlBuilder apiUrlBuilder,
            IConfiguration configuration,
            IMemoryCache cache) : base(clientFactory, logger, apiUrlBuilder)
        {
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<ApiResponse<List<FacilityItem>>?> GetFacilitiesAsync()
        {
            if (_cache.TryGetValue(CacheKey, out ApiResponse<List<FacilityItem>>? cached))
                return cached;

            var url = ApiUrlBuilder.BuildUrl(ApiRoutes.GetFacilities, "GetFacilitiesParams");

            var response = await GetAsync<ApiResponse<List<FacilityItem>>>(url);

            if (response != null)
            {
                _cache.Set(CacheKey, response, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
                });
            }

            return response;
        }
    }
}
