using Newtonsoft.Json;

namespace Book_An_Appointment1.Models.Appointment
{
    public class AppointmentDetailItem
    {
        [JsonProperty("appointmentId")]
        public string? AppointmentId { get; set; }

        [JsonProperty("facilityId")]
        public int FacilityId { get; set; }

        [JsonProperty("facilityName")]
        public string? FacilityName { get; set; }

        [JsonProperty("registrationNo")]
        public string? RegistrationNo { get; set; }

        [JsonProperty("patientName")]
        public string? PatientName { get; set; }

        [JsonProperty("doctorName")]
        public string? DoctorName { get; set; }

        [JsonProperty("doctorId")]
        public string? DoctorId { get; set; }

        [JsonProperty("specializationName")]
        public string? SpecializationName { get; set; }

        [JsonProperty("dateofBirth")]
        public string? DateOfBirth { get; set; }

        [JsonProperty("gender")]
        public string? Gender { get; set; }

        [JsonProperty("visitType")]
        public string? VisitType { get; set; }

        [JsonProperty("paymentStatus")]
        public string? PaymentStatus { get; set; }

        [JsonProperty("remarks")]
        public string? Remarks { get; set; }

        [JsonProperty("app_Starttime")]
        public string? AppStartTime { get; set; }

        [JsonProperty("app_Totime")]
        public string? AppToTime { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("teleconsultation")]
        public string? Teleconsultation { get; set; }
    }
}
