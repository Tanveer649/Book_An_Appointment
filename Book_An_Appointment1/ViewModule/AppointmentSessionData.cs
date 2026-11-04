using System.Text.Json.Serialization;
using Book_An_Appointment1.Models.Patient;

namespace Book_An_Appointment1.ViewModule
{
    public class AppointmentSessionData
    {
        // ── Page 1 — Appointment ──────────────────────────
        public int FacilityId { get; set; }
        public int DoctorId { get; set; }
        public int SpecialityId { get; set; }
        public int HospitalLocationId { get; set; }
        public string DoctorName { get; set; } = "";
        public string DoctorInitials { get; set; } = "";
        public string Specialisation { get; set; } = "";
        public string Hospital { get; set; } = "";
        public string SlotDate { get; set; } = "";
        public string SlotTime { get; set; } = "";
        public string SlotDisplayText { get; set; } = "";
        public decimal ConsultationFee { get; set; }
        public decimal RegistrationFee { get; set; }

        // ── Common (Page 2) ────────────────────────────────
        public string PatientType { get; set; } = "new";
        public string? AppointmentNotes { get; set; }
        public bool ConsentAccepted { get; set; }
        public bool ConsentMarketing { get; set; }

        // ── Separate Patient Data ─────────────────────────
        public NewPatientData NewPatient { get; set; } = new();
        public RegisteredPatientData RegisteredPatient { get; set; } = new();
        public class NewPatientData
        {
            public string? TitleName { get; set; }
            public int TitleId { get; set; }
            public string FirstName { get; set; } = "";
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = "";
            public string DOB { get; set; } = "";
            public string Gender { get; set; } = "";
            public string? Email { get; set; }
            public string? BloodGroup { get; set; }
            public string CountryCode { get; set; } = "+91";
            public string Mobile { get; set; } = "";
            public bool IsMobileVerified { get; set; }
            public string? Address { get; set; }
            public string? CountryName { get; set; }
            public int CountryId { get; set; }
            public string? StateName { get; set; }
            public int StateId { get; set; }
            public string? CityName { get; set; }
            public int CityId { get; set; }
            public string? Pincode { get; set; }
        }

        public class RegisteredPatientData
        {
            public string Mobile { get; set; } = "";
            public bool IsMobileVerified { get; set; }

            // Selected patient details for the next API
            public string? GuestPatientId { get; set; }
            public int RegistrationId { get; set; }
            public string? RegistrationNo { get; set; }

            public string? EncounterId { get; set; }
            public string? EncounterNo { get; set; }
            public string? HospitalLocationId { get; set; }
            public int FacilityId { get; set; }

            public string? PatientName { get; set; }
            public string? TitleName { get; set; }
            public int TitleId { get; set; }
            public string FirstName { get; set; } = "";
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = "";

            public string Gender { get; set; } = "";
            public string DOB { get; set; } = "";
            public string? Age { get; set; }
            public string? Email { get; set; }

            public string? PatientType { get; set; }
            public bool IsUnRegPat { get; set; }
            public string? TcFacilityId { get; set; }

            // Top-level OTP API response fields
            public string? Otp { get; set; }
            public bool IsUserExist { get; set; }
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? ApiResponseGender { get; set; }
            public string? ApiResponseDob { get; set; }
            public string? EmailId { get; set; }
            public bool IsHisRegistered { get; set; }
            public string? VerifyPatinetListCount { get; set; }

            // RegisteredPatientInfo defined on modals/patientOTP/VerifyOtpResponseData
            public List<RegisteredPatientInfo> PatientList { get; set; } = new();

            // Position of the chosen family member in PatientList (-1 = none). Not the UHID:
            // a patient with an account but no appointment booked yet has UHID "0", and several can share it.
            public int SelectedIndex { get; set; } = -1;
        }
    }
}