using Book_An_Appointment1.API.Clients;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace BookAppointmentPortal.Api.Clients;

public class BaseApiClient
{
    protected readonly IHttpClientFactory ClientFactory;
    protected readonly ILogger Logger;
    protected readonly ApiUrlBuilder ApiUrlBuilder;
    public BaseApiClient( IHttpClientFactory clientFactory, ILogger logger, ApiUrlBuilder apiUrlBuilder)
    {
        ClientFactory = clientFactory;
        Logger = logger;
        ApiUrlBuilder = apiUrlBuilder;
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


    protected async Task<T?> PostAsync<T>(string url, object body)
    {
        try
        {
            var client = ClientFactory.CreateClient("ApiClient");

            Logger.LogInformation("API POST Request: {Url}", url);

            var json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Logger.LogInformation(
                "API POST Response: {StatusCode} | {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"API Failed: {response.StatusCode}");

            return JsonConvert.DeserializeObject<T>(responseContent);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "API POST Error: {Url}", url);
            throw;
        }
    }
}

