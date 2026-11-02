using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Book_An_Appointment1.Models.Patient
{
    public class VerifyOtpResponseData
    {
        [JsonPropertyName("mobileNo")]
        public string? MobileNo { get; set; }

        [JsonPropertyName("otp")]
        public string? Otp { get; set; }

        [JsonPropertyName("isUserExist")]
        public bool IsUserExist { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("dob")]
        public string? Dob { get; set; }

        [JsonPropertyName("emailId")]
        public string? EmailId { get; set; }

        [JsonPropertyName("isHisRegistered")]
        public bool IsHisRegistered { get; set; }

        [JsonPropertyName("verifyPatinetListCount")]
        public string? VerifyPatinetListCount { get; set; }

        [JsonPropertyName("patientList")]
        public List<RegisteredPatientInfo> PatientList { get; set; } = new();

    }
    public class VerifyOtpResponseModel
    {
        public VerifyOtpResponseData? Data { get; set; }
        public string Message { get; set; } = "";
        public string? ErrorCode { get; set; }
        public string Status { get; set; } = "";
    }
    public class RegisteredPatientInfo
    {
        [JsonPropertyName("hospitalLocationId")]
        public string? HospitalLocationId { get; set; }

        [JsonPropertyName("facilityID")]
        public string? FacilityID { get; set; }

        [JsonPropertyName("registrationId")]
        public int RegistrationId { get; set; }

        [JsonPropertyName("registrationNo")]
        public string? RegistrationNo { get; set; }

        [JsonPropertyName("encounterId")]
        public string? EncounterId { get; set; }

        [JsonPropertyName("encounterNo")]
        public string? EncounterNo { get; set; }

        [JsonPropertyName("patientName")]
        public string? PatientName { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("dob")]
        public string? Dob { get; set; }

        [JsonPropertyName("age")]
        public string? Age { get; set; }

        [JsonPropertyName("emailId")]
        public string? EmailId { get; set; }

        [JsonPropertyName("mobileNo")]
        public string? MobileNo { get; set; }

        [JsonPropertyName("patientType")]
        public string? PatientType { get; set; }

        [JsonPropertyName("isUnRegPat")]
        public bool IsUnRegPat { get; set; }

        [JsonPropertyName("tcFacilityId")]
        public string? TcFacilityId { get; set; }
    }
}
