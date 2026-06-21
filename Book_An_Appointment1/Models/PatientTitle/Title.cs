
using Newtonsoft.Json;

namespace Book_An_Appointment1.Models.PatientTitle
{
    public class TitleItem
    {
        [JsonProperty("TitleID")]
        public int TitleId { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; } = "";

        [JsonProperty("Gender")]
        public string Gender { get; set; } = "";
    }

    public class TitleResponse
    {
        [JsonProperty("TitleList")]
        public List<TitleItem> TitleList { get; set; } = new();

        [JsonProperty("status")]
        public string Status { get; set; } = "";

        [JsonProperty("message")]
        public string Message { get; set; } = "";
    }
}
