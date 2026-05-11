using System.Net;
using System.Net.Http.Headers;
using Book_An_Appointment1.Services;

namespace Book_An_Appointment1.API.Handlers
{
    public class AuthHandler : DelegatingHandler
    {
        private readonly TokenService _tokenService;

        public AuthHandler(TokenService tokenService)
        {
            _tokenService = tokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenService.GetTokenAsync(); 

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); 

            var response = await base.SendAsync(request, cancellationToken); 

            if (response.StatusCode == HttpStatusCode.Unauthorized) 
            { 
                _tokenService.ClearToken(); 
            }
            return response;
        }
    }
}
