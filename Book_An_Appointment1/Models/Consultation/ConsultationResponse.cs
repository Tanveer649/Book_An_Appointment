namespace Book_An_Appointment1.Models.Consultation
{
    //public class ConsultationResponse
    //{
    //    public List<ConsultationItem> Data { get; set; } = new();
    //}

    //public class ConsultationItem
    //{
    //    public int ServiceId { get; set; }

    //    public string ServiceName { get; set; }

    //    public string Price { get; set; }

    //    public string VisitType { get; set; }

    //    public string ServiceType { get; set; }
    //    //public IEnumerable<object> Data { get; internal set; }
    //}
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