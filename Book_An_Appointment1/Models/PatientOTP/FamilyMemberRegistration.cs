using Newtonsoft.Json;

namespace Book_An_Appointment1.Models.Patient
{
    // Body of /api/VCP/MobSaveRegistrationForPatient (names exactly as the API expects; strings except the two numbers)
    public class FamilyMemberRegistrationRequest
    {
        [JsonProperty("inyHospitalLocationId")] public int HospitalLocationId { get; set; }   // number (tinyint in the procedure)
        [JsonProperty("FacilityID")] public string FacilityId { get; set; } = "";
        [JsonProperty("FirstName")] public string FirstName { get; set; } = "";
        [JsonProperty("MiddleName")] public string MiddleName { get; set; } = " ";   // API sample sends " " when empty
        [JsonProperty("LastName")] public string LastName { get; set; } = "";
        [JsonProperty("DOB")] public string Dob { get; set; } = "";                 // yyyy-MM-dd
        [JsonProperty("Gender")] public string Gender { get; set; } = "";           // code from AppointmentApi:GenderCodes
        [JsonProperty("Mobile")] public string Mobile { get; set; } = "";
        [JsonProperty("Email")] public string Email { get; set; } = "";
        [JsonProperty("PaymentFlag")] public string PaymentFlag { get; set; } = "0";
        [JsonProperty("UniqueValue")] public string UniqueValue { get; set; } = "";
        [JsonProperty("TitleId")] public string TitleId { get; set; } = "";
        [JsonProperty("Address")] public string Address { get; set; } = "";
        [JsonProperty("CountryId")] public string CountryId { get; set; } = "";
        [JsonProperty("StateId")] public string StateId { get; set; } = "";
        [JsonProperty("CityId")] public string CityId { get; set; } = "";
        [JsonProperty("AreaId")] public string AreaId { get; set; } = "";
        [JsonProperty("ZipCode")] public string ZipCode { get; set; } = "";
        [JsonProperty("OTP")] public string Otp { get; set; } = "";
        // 1 = family member of this mobile. Sent as the NUMBER 1, never the string "1": the API reads it into a
        // bool, the string "1" cannot be converted to a bool, the value is dropped and the procedure gets
        // @bitIsFamilyMember=0 (member not added). A number works whether the API property is bool, int or string.
        [JsonProperty("IsFamilyMember")] public int IsFamilyMember { get; set; } = 1;
    }

    // Result of MobSaveRegistrationForPatient: Patient is the new family member (same shape as the verify-OTP list)
    public class FamilyMemberRegistrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public RegisteredPatientInfo? Patient { get; set; }
    }
}
