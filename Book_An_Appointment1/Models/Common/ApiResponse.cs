using Newtonsoft.Json;
namespace Book_An_Appointment1.Models.Common
{
    public class ApiResponse<T>
    {
        [JsonProperty("data")]
        public T Data { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
    }
}
