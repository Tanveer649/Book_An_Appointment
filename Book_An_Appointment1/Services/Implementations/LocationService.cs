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
    public LocationService(
        IHttpClientFactory clientFactory,
        ILogger<LocationService> logger,
        ApiUrlBuilder apiUrlBuilder,
        IMemoryCache cache)
        : base(clientFactory, logger, apiUrlBuilder)
    {
        _cache = cache;
    }

    public async Task<ApiResponse<List<CountryItem>>?> GetCountriesAsync()
    {
        //var url = ApiUrlBuilder.BuildUrl(
        //    ApiRoutes.GetCountryList, "GetCountryListParams");
        //return await GetAsync<ApiResponse<List<CountryItem>>>(url);

        if (_cache.TryGetValue(CountryCacheKey,
               out ApiResponse<List<CountryItem>>? cached))
        {
            Logger.LogInformation("Countries — cache hit");
            return cached;
        }

        var result = await GetAsync<ApiResponse<List<CountryItem>>>(ApiRoutes.GetCountryList);
        if (result != null)
        {
            _cache.Set(CountryCacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });
            Logger.LogInformation("Countries — cached for 24 hours");
        }
        return result;
    }

    public async Task<ApiResponse<List<StateItem>>?> GetStatesAsync(int countryId)
    {
        //var url = ApiUrlBuilder.BuildUrl(
        //    ApiRoutes.GetStateList, "GetStateListParams",
        //    new Dictionary<string, string>
        //    {
        //        { "countryId", countryId.ToString() }
        //    });
        //return await GetAsync<ApiResponse<List<StateItem>>>(url);

        var url = $"{ApiRoutes.GetStateList}?countryId={countryId}";
        return await GetAsync<ApiResponse<List<StateItem>>>(url);
    }

    public async Task<ApiResponse<List<CityItem>>?> GetCitiesAsync(int stateId)
    {
        //var url = ApiUrlBuilder.BuildUrl(
        //    ApiRoutes.GetCityList, "GetCityListParams",
        //    new Dictionary<string, string>
        //    {
        //        { "stateId", stateId.ToString() }
        //    });
        //return await GetAsync<ApiResponse<List<CityItem>>>(url);


        var url = $"{ApiRoutes.GetCityList}?stateId={stateId}";
        return await GetAsync<ApiResponse<List<CityItem>>>(url);
    }
}