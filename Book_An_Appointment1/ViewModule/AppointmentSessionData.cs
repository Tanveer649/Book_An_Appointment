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
            public string? GuestPatientId { get; set; }
            public string? RegistrationNo { get; set; }
            public string? TitleName { get; set; }
            public int TitleId { get; set; }
            public string FirstName { get; set; } = "";
            public string? MiddleName { get; set; }
            public string LastName { get; set; } = "";
            public string Gender { get; set; } = "";
            public string DOB { get; set; } = "";
            public string? Email { get; set; }
            public int FacilityId { get; set; }
        }
    }
}