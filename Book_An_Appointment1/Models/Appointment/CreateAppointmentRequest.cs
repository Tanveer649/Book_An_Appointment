using Newtonsoft.Json;

namespace Book_An_Appointment1.Models.Appointment
{
    public class CreateAppointmentRequest
    {
        [JsonProperty("facilityCode")]
        public string FacilityCode { get; set; } = "";

        // 0 for new (unregistered) patient
        [JsonProperty("registrationNo")]
        public long RegistrationNo { get; set; }

        [JsonProperty("titleId")]
        public string TitleId { get; set; } = "";

        [JsonProperty("firstName")]
        public string FirstName { get; set; } = "";

        [JsonProperty("middleName")]
        public string MiddleName { get; set; } = "";

        [JsonProperty("lastName")]
        public string LastName { get; set; } = "";

        [JsonProperty("doctorRegistrationNo")]
        public string DoctorRegistrationNo { get; set; } = "";

        [JsonProperty("doctorId")]
        public string DoctorId { get; set; } = "";

        // Format: yyyy-MM-dd HH:mm
        [JsonProperty("appFrom_DateTime")]
        public string AppFromDateTime { get; set; } = "";

        [JsonProperty("appTo_DateTime")]
        public string AppToDateTime { get; set; } = "";

        [JsonProperty("walkInPatient")]
        public string WalkInPatient { get; set; } = "";

        // Format: yyyy-MM-dd
        [JsonProperty("dob")]
        public string Dob { get; set; } = "";

        // Code from AppointmentApi:GenderCodes
        [JsonProperty("gender")]
        public string Gender { get; set; } = "";

        [JsonProperty("mobileNo")]
        public string MobileNo { get; set; } = "";

        [JsonProperty("remarks")]
        public string Remarks { get; set; } = "";

        [JsonProperty("address1")]
        public string Address1 { get; set; } = "";

        [JsonProperty("address2")]
        public string Address2 { get; set; } = "";

        [JsonProperty("cityCode")]
        public string CityCode { get; set; } = "";

        [JsonProperty("stateCode")]
        public string StateCode { get; set; } = "";

        [JsonProperty("countryCode")]
        public string CountryCode { get; set; } = "";

        [JsonProperty("pinCode")]
        public string PinCode { get; set; } = "";

        [JsonProperty("source")]
        public string Source { get; set; } = "";

        [JsonProperty("appointmentType")]
        public string AppointmentType { get; set; } = "";

        [JsonProperty("sourceAppointmentId")]
        public string SourceAppointmentId { get; set; } = "";

        [JsonProperty("guestPatientId")]
        public string GuestPatientId { get; set; } = "0";

        // Pay now = true, Pay on arrival = false
        [JsonProperty("IsOnlinePayment")]
        public bool IsOnlinePayment { get; set; }
    }
}
