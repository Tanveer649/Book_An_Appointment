using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace BookAppointmentPortal.Api.Clients;

public class BaseApiClient
{
    protected readonly IHttpClientFactory ClientFactory;
    protected readonly ILogger Logger;

    public BaseApiClient( IHttpClientFactory clientFactory, ILogger logger)
    {
        ClientFactory = clientFactory;
        Logger = logger;
    }

    protected async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            var client = ClientFactory.CreateClient("ApiClient");

            Logger.LogInformation("API Request: {Url}", url);

            var response = await client.GetAsync(url);

            var content = await response.Content.ReadAsStringAsync();

            Logger.LogInformation(
                "API Response: {StatusCode} | {Content}",
                response.StatusCode,
                content);

            if (!response.IsSuccessStatusCode)
                throw new Exception(
                    $"API Failed: {response.StatusCode}");

            return JsonConvert.DeserializeObject<T>(content);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "API Error");

            throw;
        }
    }
}

