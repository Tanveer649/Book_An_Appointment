using Newtonsoft.Json;
namespace Book_An_Appointment1.Models.Location
{
    public class CountryItem
    {
        [JsonProperty("countryId")]
        public int CountryId { get; set; }

        [JsonProperty("countryName")]
        public string CountryName { get; set; } = "";

        [JsonProperty("sequenceNo")]
        public string SequenceNo { get; set; } = "";
    }

    public class StateItem
    {
        [JsonProperty("countryId")]
        public int CountryId { get; set; }

        [JsonProperty("stateId")]
        public int StateId { get; set; } 

        [JsonProperty("stateName")]
        public string StateName { get; set; } = "";
    }

    public class CityItem
    {
        [JsonProperty("stateId")]
        public int StateId { get; set; }

        [JsonProperty("cityId")]
        public int CityId { get; set; }

        [JsonProperty("cityName")]
        public string CityName { get; set; } = "";
    }
}
