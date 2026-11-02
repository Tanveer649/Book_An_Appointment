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
    }
}
