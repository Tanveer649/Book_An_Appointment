using System.Net.Http.Json;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Token;
using Book_An_Appointment1.Services.Interfaces;

namespace Book_An_Appointment1.Services
{
    public class TokenService : ITokenService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        public TokenService(
            IHttpContextAccessor contextAccessor,
            IHttpClientFactory clientFactory,
            IConfiguration configuration)
        {
            _contextAccessor = contextAccessor;
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        public async Task<string> GetTokenAsync()
        {
            var session = _contextAccessor.HttpContext?.Session;

            if (session == null)
                throw new Exception("Session unavailable.");

            var existingToken = session.GetString("AccessToken");

            if (!string.IsNullOrWhiteSpace(existingToken))
                return existingToken;

            var client = _clientFactory.CreateClient("TokenClient");

            var request = new TokenRequest
            {
                UserName = _configuration["ApiSettings:UserName"],
                Password = _configuration["ApiSettings:Password"]
            };

            var response =
                await client.PostAsJsonAsync(ApiRoutes.GetToken, request);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<TokenResponse>();

            if (string.IsNullOrWhiteSpace(result?.AccessToken))
                throw new Exception("Token not received.");

            session.SetString("AccessToken", result.AccessToken);

            return result.AccessToken;
        }

        public void ClearToken()
        {
            _contextAccessor.HttpContext?.Session
                ?.Remove("AccessToken");
        }
    }
}