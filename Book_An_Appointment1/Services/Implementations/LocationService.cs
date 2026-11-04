using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Location;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;
using Microsoft.Extensions.Caching.Memory;

namespace Book_An_Appointment1.Services.Implementations;

public class LocationService : BaseApiClient, ILocationService
{
    private readonly IMemoryCache _cache;
    private const string CountryCacheKey = "countries_data";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);   // master data, changes rarely

    public LocationService(
        IHttpClientFactory clientFactory,
        ILogger<LocationService> logger,
        ApiUrlBuilder apiUrlBuilder,
        IMemoryCache cache)
        : base(clientFactory, logger, apiUrlBuilder)
    {
        _cache = cache;
    }

    public Task<ApiResponse<List<CountryItem>>?> GetCountriesAsync() =>
        GetCachedAsync<CountryItem>(CountryCacheKey, ApiRoutes.GetCountryList);

    public Task<ApiResponse<List<StateItem>>?> GetStatesAsync(int countryId) =>
        GetCachedAsync<StateItem>($"states_{countryId}", $"{ApiRoutes.GetStateList}?countryId={countryId}");

    public Task<ApiResponse<List<CityItem>>?> GetCitiesAsync(int stateId) =>
        GetCachedAsync<CityItem>($"cities_{stateId}", $"{ApiRoutes.GetCityList}?stateId={stateId}");

    private async Task<ApiResponse<List<T>>?> GetCachedAsync<T>(string cacheKey, string url)
    {
        if (_cache.TryGetValue(cacheKey, out ApiResponse<List<T>>? cached))
        {
            Logger.LogInformation("{Key} — cache hit", cacheKey);
            return cached;
        }

        var result = await GetAsync<ApiResponse<List<T>>>(url);

        // Empty / failed answers are not cached, so a temporary API problem is not remembered
        if (result?.Data?.Count > 0)
        {
            _cache.Set(cacheKey, result, CacheLifetime);
            Logger.LogInformation("{Key} — cached for 24 hours", cacheKey);
        }
        return result;
    }
}
