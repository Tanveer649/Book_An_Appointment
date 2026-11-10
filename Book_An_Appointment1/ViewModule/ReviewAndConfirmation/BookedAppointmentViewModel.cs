using System.Text.Json.Serialization;

namespace Book_An_Appointment1.ViewModule.ReviewAndConfirmation
{
    // Shown in the "Appointment booked" popup (Pay on arrival)
    public class BookedAppointmentViewModel
    {
        public string AppointmentId { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string? RegistrationNo { get; set; }
        public string Mobile { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string Specialisation { get; set; } = "";
        public string HospitalName { get; set; } = "";
        public string AppointmentDate { get; set; } = "";      // 04 Nov 2026
        public string AppointmentDay { get; set; } = "";       // Wednesday
        public string AppointmentTime { get; set; } = "";
        public string? VisitType { get; set; }
        public string? Status { get; set; }
        public string AmountPayable { get; set; } = "";
        public string PaymentMode { get; set; } = "Pay at hospital";

        // Appointment slip (Services/Implementations/AppointmentSlipService.cs)
        public string? SlipUrl { get; set; }            // page — also what the QR code opens
        public string? ViewSlipUrl { get; set; }        // "View slip" button (null = SlipSettings:EnableViewSlip off)
        public string? PdfUrl { get; set; }
        public string? PrintUrl { get; set; }
        public string? IcsUrl { get; set; }             // Apple / Outlook / any calendar app
        public string? GoogleCalendarUrl { get; set; }
        public string? QrSvg { get; set; }

        // Slot times for the calendar entry — not sent to the browser
        [JsonIgnore] public DateTime? StartsAt { get; set; }
        [JsonIgnore] public DateTime? EndsAt { get; set; }
    }
}
