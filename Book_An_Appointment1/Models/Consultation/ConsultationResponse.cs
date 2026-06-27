namespace Book_An_Appointment1.Models.Consultation
{
    public class ConsultationItem
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string VisitType { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string ServiceDiscountAmount { get; set; } = string.Empty;
        public string NetAmount { get; set; } = string.Empty;
    }
}