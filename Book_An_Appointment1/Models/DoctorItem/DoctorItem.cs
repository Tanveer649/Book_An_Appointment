using Newtonsoft.Json;

namespace Book_An_Appointment1.Models.DoctorItem
{
    public class DoctorItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public int DoctorId => int.TryParse(Id, out var id) ? id : 0;

        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [JsonProperty("education")]
        public string Education { get; set; }

        [JsonProperty("designation")]
        public string Designation { get; set; }

        [JsonProperty("specialisationName")]
        public string SpecialisationName { get; set; }

        [JsonProperty("specialisationId")]
        public string SpecialisationId { get; set; }

        [JsonProperty("departmentName")]
        public string DepartmentName { get; set; }

        [JsonProperty("workexperience")]
        public string WorkExperience { get; set; }

        [JsonProperty("professionalStatement")]
        public string ProfessionalStatement { get; set; }

        [JsonProperty("aboutUS")]
        public string AboutUS { get; set; }

        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("review")]
        public string Review { get; set; }

        [JsonProperty("availability")]
        public string Availability { get; set; }

        [JsonProperty("treatment")]
        public string Treatment { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("doctorFee")]
        public string DoctorFee { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("mobile")]
        public string Mobile { get; set; }

        [JsonProperty("isTeleconsultation")]
        public string IsTeleconsultation { get; set; }

        [JsonProperty("awards")]
        public string Awards { get; set; }

        [JsonProperty("consultingHospitals")]
        public List<ConsultingHospital> ConsultingHospitals { get; set; } = new();

        public int HospitalLocationId =>
            int.TryParse(ConsultingHospitals.FirstOrDefault()?.OrganizationId, out var id) ? id : 0;

        public string Name => FullName ?? "N/A";
        public string Experience => !string.IsNullOrWhiteSpace(WorkExperience) ? WorkExperience : "N/A";
        public string Description => !string.IsNullOrWhiteSpace(AboutUS) ? AboutUS : "N/A";
        public string ImageUrl => !string.IsNullOrWhiteSpace(Icon) ? Icon : "/images/default-doctor.png";
        public string Timing => !string.IsNullOrWhiteSpace(Availability)
            ? DateTime.TryParse(Availability, out var dt) ? dt.ToString("hh:mm tt") : Availability
            : "N/A";
        public bool IsTeleconsult => IsTeleconsultation == "1";
    }

    public class ConsultingHospital
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("facilityName")]
        public string FacilityName { get; set; }

        [JsonProperty("organizationId")]
        public string OrganizationId { get; set; }

        [JsonProperty("cityName")]
        public string CityName { get; set; }

        [JsonProperty("stateName")]
        public string StateName { get; set; }
    }



    //public class DoctorItem
    //{
    //    [JsonProperty("id")]
    //    public string Id { get; set; }  // ← yahan lagao

    //    public int DoctorId => int.TryParse(Id, out var id) ? id : 0;  // computed

    //    [JsonProperty("fullName")]
    //    public string FullName { get; set; }

    //    [JsonProperty("education")]
    //    public string Education { get; set; }

    //    [JsonProperty("designation")]
    //    public string Designation { get; set; }

    //    [JsonProperty("specialisationName")]
    //    public string SpecialisationName { get; set; }

    //    [JsonProperty("aboutUS")]
    //    public string AboutUS { get; set; }

    //    [JsonProperty("rating")]
    //    public string Rating { get; set; }

    //    [JsonProperty("review")]
    //    public string Review { get; set; }

    //    [JsonProperty("availability")]
    //    public string Availability { get; set; }

    //    [JsonProperty("treatment")]
    //    public string Treatment { get; set; }

    //    [JsonProperty("icon")]
    //    public string Icon { get; set; }

    //    [JsonProperty("doctorFee")]
    //    public string DoctorFee { get; set; }

    //    public int HospitalLocationId { get; set; }

    //    public string Name => FullName ?? "N/A";
    //    public string Experience => Education ?? "N/A";
    //    public string Description => AboutUS ?? "N/A";
    //    public string Timing => Availability ?? "N/A";
    //    public string ImageUrl => Icon ?? "/images/default-doctor.png";
    //}
}