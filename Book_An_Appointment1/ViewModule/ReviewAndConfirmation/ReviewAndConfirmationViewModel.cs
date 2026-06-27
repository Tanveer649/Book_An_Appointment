namespace Book_An_Appointment1.ViewModule.ReviewAndConfirmation
{
    public class ReviewAndConfirmationViewModel
    {
        // Appointment
        public string Hospital { get; set; } = "";
        public string Specialisation { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string SlotDisplayText { get; set; } = "";
        public string SlotDate { get; set; } = "";
        public string SlotTime { get; set; } = "";
        public decimal ConsultationFee { get; set; }

        // Patient
        public string PatientType { get; set; } = "new";
        public string Title { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = "";
        public string Gender { get; set; } = "";
        public string DOB { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string CountryCode { get; set; } = "+91";
        public string? Email { get; set; }
        public string? BloodGroup { get; set; }
        public string? Address { get; set; }
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public int CityId { get; set; }
        public string? Pincode { get; set; }

        // Notes + Consent
        public string? AppointmentNotes { get; set; }
        public bool ConsentTerms { get; set; }
        public bool ConsentMarketing { get; set; }


        // Payment Setting 

        public bool EnablePayNow { get; set; }
        public bool EnablePayOnArrival { get; set; }


        // Helpers
        public string FullName =>
            $"{Title} {FirstName} {MiddleName} {LastName}"
                .Replace("  ", " ").Trim();

        public string FormattedDOB
        {
            get
            {
                if (DateTime.TryParse(DOB, out var d))
                    return d.ToString("dd MMM yyyy");
                return DOB;
            }
        }

        public string PatientTypeDisplay =>
            PatientType == "registered" ? "Registered Patient" : "New Patient";

        public string? RegistrationNo { get; set; }

    }
}
