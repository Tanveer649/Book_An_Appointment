using Newtonsoft.Json;
namespace Book_An_Appointment1.Models.Patient
{
    public class VerifyOtpResponseData
    {
        [JsonProperty("mobileNo")]
        public string MobileNo { get; set; } = "";

        [JsonProperty("otp")]
        public string Otp { get; set; } = "";

        [JsonProperty("isUserExist")]
        public bool IsUserExist { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("gender")]
        public string Gender { get; set; } = "";

        [JsonProperty("dob")]
        public string Dob { get; set; } = "";

        [JsonProperty("emailId")]
        public string EmailId { get; set; } = "";

        [JsonProperty("isHisRegistered")]
        public bool IsHisRegistered { get; set; }

        [JsonProperty("verifyPatinetListCount")]
        public string VerifyPatientListCount { get; set; } = "";

        [JsonProperty("patinetList")]
        public List<PatientListItem> PatientList { get; set; } = new();

    }
    public class VerifyOtpResponseModel
    {
        public VerifyOtpResponseData? Data { get; set; }
        public string Message { get; set; } = "";
        public string? ErrorCode { get; set; }
        public string Status { get; set; } = "";
    }
    public class PatientListItem
    {
        [JsonProperty("GuestPateintId")]
        public string GuestPatientId { get; set; } = "";

        [JsonProperty("registrationNo")]
        public string RegistrationNo { get; set; } = "";

        [JsonProperty("patientName")]
        public string PatientName { get; set; } = "";

        [JsonProperty("titleId")]
        public string TitleId { get; set; } = "";

        [JsonProperty("titleName")]
        public string TitleName { get; set; } = "";

        [JsonProperty("firstName")]
        public string FirstName { get; set; } = "";

        [JsonProperty("middleName")]
        public string MiddleName { get; set; } = "";

        [JsonProperty("lastname")]
        public string LastName { get; set; } = "";

        [JsonProperty("gender")]
        public string Gender { get; set; } = "";

        [JsonProperty("dob")]
        public string Dob { get; set; } = "";

        [JsonProperty("age")]
        public string Age { get; set; } = "";

        [JsonProperty("emailId")]
        public string EmailId { get; set; } = "";

        [JsonProperty("mobileNo")]
        public string MobileNo { get; set; } = "";

        [JsonProperty("facilityID")]
        public string FacilityId { get; set; } = "";

        [JsonProperty("icon")]
        public string Icon { get; set; } = "";

        [JsonProperty("isUnRegPat")]
        public bool IsUnRegPat { get; set; }
    }
}
