using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.PatientTitle;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;
using Microsoft.Extensions.Caching.Memory;

namespace Book_An_Appointment1.Services.Implementations
{
    public class TitleService : BaseApiClient, ITitleService
    {
        private readonly IMemoryCache _cache;
        private const string TitleCacheKey = "titles_data";
        public TitleService(
          IHttpClientFactory clientFactory,
          ILogger<TitleService> logger,
          ApiUrlBuilder apiUrlBuilder,
          IMemoryCache cache)
          : base(clientFactory, logger, apiUrlBuilder)
        {
            _cache = cache;
        }

        public async Task<TitleResponse?> GetTitlesAsync()
        {
            if (_cache.TryGetValue(TitleCacheKey, out TitleResponse? cached))
            {
                Logger.LogInformation("Titles — cache hit");
                return cached;
            }
            var result = await GetAsync<TitleResponse>(ApiRoutes.GetTitleList);

            if (result != null)
            {
                _cache.Set(TitleCacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                });
                Logger.LogInformation("Titles — cached for 24 hours");
            }
            return result;
        }
    }
}
