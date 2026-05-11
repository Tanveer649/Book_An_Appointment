using Newtonsoft.Json;

namespace Book_An_Appointment1.Models.Slot
{
    public class DoctorSlotResponse
    {
        [JsonProperty("facilityId")]
        public int FacilityId { get; set; }

        [JsonProperty("facilityCode")]
        public string FacilityCode { get; set; }

        [JsonProperty("doctorRegistrationNo")]
        public string DoctorRegistrationNo { get; set; }

        [JsonProperty("onLinePayment")]
        public int OnLinePayment { get; set; }

        [JsonProperty("offLinePayment")]
        public int OffLinePayment { get; set; }

        [JsonProperty("slotsDetails")]
        public List<SlotResponse> SlotsDetails { get; set; } = new();
    }

    public class SlotResponse
    {
        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        [JsonProperty("endTime")]
        public string EndTime { get; set; }

        [JsonProperty("statusText")]
        public string StatusText { get; set; }

        [JsonProperty("reservationType")]
        public string ReservationType { get; set; }

        [JsonProperty("rowNumber")]
        public int RowNumber { get; set; }

        [JsonProperty("pageNumber")]
        public int PageNumber { get; set; }

        [JsonProperty("nextPageNumber")]
        public int NextPageNumber { get; set; }

        [JsonProperty("totalRecordsCount")]
        public int TotalRecordsCount { get; set; }
    }

}
