namespace Book_An_Appointment1.API.EndPoints
{
    public static class ApiRoutes
    {
        public const string GetToken = "/api/Token/GetToken";
        public const string GetFacilities = "/api/VCP/GetFacilityDetails";
        public const string GetSpecialities = "/api/VCP/GetSpecialityList";
        public const string GetDoctors = "/api/VCP/GetDoctorList";
        public const string GetDoctorSlots = "/api/VCP/GetDoctorSlots";
        public const string GetConsultationCharges = "/api/VCP/GetConsultationCharges";
        public const string GenerateOtp = "/api/VCP/GenerateOTPForSignup";
        public const string VerifyOtp = "/api/VCP/VerifyPatientOTP";
        public const string GetCountryList = "/api/vcp/GetCountryList";
        public const string GetStateList = "/api/vcp/GetStateList";
        public const string GetCityList = "/api/vcp/GetCityList";
        public const string GetTitleList = "/api/vcp/GetTitles";
        public const string GenerateOtpForPatient = "/api/VCP/GenerateOTPForPatient";
    }
}
