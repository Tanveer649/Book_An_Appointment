namespace Book_An_Appointment1.Models.Consultation
{
    public class ConsultationResponse
    {
        public List<ConsultationItem> Data { get; set; } = new();
    }

    public class ConsultationItem
    {
        public int ServiceId { get; set; }

        public string ServiceName { get; set; }

        public string Price { get; set; }

        public string VisitType { get; set; }

        public string ServiceType { get; set; }
    }
}