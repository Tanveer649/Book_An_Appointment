using Newtonsoft.Json;

namespace Book_An_Appointment1.Models.Speciality
{
    public class SpecialityItem
    {
        public string Id { get; set; }

        [JsonProperty("departmentId")]
        public string DepartmentId { get; set; }

        [JsonProperty("name")]
        public string SpecialisationName { get; set; }

        [JsonProperty("isTeleconsultation")]
        public bool IsTeleconsultation { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }
}
