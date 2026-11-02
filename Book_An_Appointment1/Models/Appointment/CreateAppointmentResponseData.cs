using Newtonsoft.Json;

namespace Book_An_Appointment1.Models.Appointment
{
    public class CreateAppointmentResponseData
    {
        // "0" when the API rejects the request
        [JsonProperty("appointmentId")]
        public string AppointmentId { get; set; } = "";
    }
}
