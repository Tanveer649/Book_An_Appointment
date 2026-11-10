namespace Book_An_Appointment1.Models.Appointment
{
    // Everything the appointment slip shows (web page, PDF, calendar entry).
    // Built from GetAppointmentByID + the few values carried in the signed slip link.
    public class AppointmentSlip
    {
        public string AppointmentId { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string? RegistrationNo { get; set; }
        public string Mobile { get; set; } = "";             // masked, e.g. XXXXXX6293
        public string DoctorName { get; set; } = "";
        public string Specialisation { get; set; } = "";
        public string HospitalName { get; set; } = "";
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public string? VisitType { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string Amount { get; set; } = "";             // "₹ 1"
        public string PaymentMode { get; set; } = "";
        public string? Remarks { get; set; }

        public string DateText => StartsAt?.ToString("dd MMM yyyy") ?? "";
        public string DayText => StartsAt?.ToString("dddd") ?? "";
        public string TimeText => StartsAt?.ToString("hh:mm tt") ?? "";
    }

    // Values sealed inside the slip link (encrypted + signed, so they cannot be read or changed)
    // Uhid / PatientName: shown when GetAppointmentByID has none (UHID 0 patients; the API often leaves patientName empty)
    public record SlipTicket(string AppointmentId, string Amount, string Mobile, string PaymentMode, string Uhid = "", string PatientName = "");
}
