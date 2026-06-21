namespace Book_An_Appointment1.Models.Patient
{
    public class OtpResponseModel
    {
        public List<OtpResponseData> Data { get; set; } = new();
        public string Message { get; set; } = "";
        public string? ErrorCode { get; set; }
        public string Status { get; set; } = "";
    }
    public class OtpResponseData
    {
        public string OtpNo { get; set; } = "";
        public string MobileNo { get; set; } = "";
    }

}
