using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Location;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface ILocationService
    {
        Task<ApiResponse<List<CountryItem>>?> GetCountriesAsync();
        Task<ApiResponse<List<StateItem>>?> GetStatesAsync (int countryId);
        Task<ApiResponse<List<CityItem>>?> GetCitiesAsync(int stateId);
    }
}
