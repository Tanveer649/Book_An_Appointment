using Newtonsoft.Json;

namespace Book_An_Appointment1.Models.Facility
{
    public class FacilityItem
    {
        [JsonProperty("hospitalLocationId")]
        public int HospitalLocationId { get; set; }

        [JsonProperty("facilityId")]
        public int FacilityId { get; set; }

        [JsonProperty("facilityCode")]
        public string FacilityCode { get; set; }

        [JsonProperty("name")]
        public string FacilityName { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("address1")]
        public string Address1 { get; set; }

        [JsonProperty("address2")]
        public string Address2 { get; set; }
    }
}

