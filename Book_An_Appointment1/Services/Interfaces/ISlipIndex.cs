namespace Book_An_Appointment1.Services.Interfaces
{
    // One issued appointment slip, kept so the patient can find it again ("Find my slip").
    // The mobile number is stored only as a salted hash. The patient name is kept so family members sharing one
    // mobile can tell their slips apart (the hospital API often returns no name for an appointment).
    public class SlipRecord
    {
        public string AppointmentId { get; set; } = "";
        public string MobileHash { get; set; } = "";
        public string MaskedMobile { get; set; } = "";
        public string Amount { get; set; } = "";
        public string PaymentMode { get; set; } = "";
        public string Uhid { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public string Specialisation { get; set; } = "";
        public DateTime? StartsAt { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public interface ISlipIndex
    {
        void Save(string mobile, SlipRecord record);

        // Slips issued for this mobile that are still valid, newest appointment first
        IReadOnlyList<SlipRecord> FindByMobile(string mobile);
    }
}
