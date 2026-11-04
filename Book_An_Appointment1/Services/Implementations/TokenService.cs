using System.Net.Http.Json;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Token;
using Book_An_Appointment1.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Book_An_Appointment1.Services
{
    // Hospital API token for the portal's own API account — the same for every visitor,
    // so it is cached once for the whole app until shortly before it expires.
    public class TokenService : ITokenService
    {
        private const string CacheKey = "api_access_token";
        private static readonly SemaphoreSlim RefreshLock = new(1, 1);   // one token request at a time

        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IMemoryCache cache,
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            ILogger<TokenService> logger)
        {
            _cache = cache;
            _clientFactory = clientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetTokenAsync()
        {
            if (_cache.TryGetValue(CacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
                return cached;

            await RefreshLock.WaitAsync();
            try
            {
                // Another request may have refreshed it while we waited
                if (_cache.TryGetValue(CacheKey, out cached) && !string.IsNullOrWhiteSpace(cached))
                    return cached;

                var userName = _configuration["ApiSettings:UserName"];
                var password = _configuration["ApiSettings:Password"];
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                    throw new InvalidOperationException("ApiSettings:UserName / Password not configured (see Docs/Security.md).");

                var client = _clientFactory.CreateClient("TokenClient");

                var response = await client.PostAsJsonAsync(ApiRoutes.GetToken,
                    new TokenRequest { UserName = userName, Password = password });

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

                if (string.IsNullOrWhiteSpace(result?.AccessToken))
                    throw new Exception("Token not received.");

                // Refresh 1 minute early; unknown lifetime → 20 minutes
                var lifetime = result.ExpiresIn > 120
                    ? TimeSpan.FromSeconds(result.ExpiresIn - 60)
                    : TimeSpan.FromMinutes(20);

                _cache.Set(CacheKey, result.AccessToken, lifetime);
                _logger.LogInformation("API token refreshed | ValidFor={Minutes} min", (int)lifetime.TotalMinutes);

                return result.AccessToken;
            }
            finally
            {
                RefreshLock.Release();
            }
        }

        // Called by AuthHandler on 401 — next call fetches a new token
        public void ClearToken()
        {
            _cache.Remove(CacheKey);
        }
    }
}
