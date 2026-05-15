using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Slot;
using Book_An_Appointment1.Services.Interfaces;
using Book_An_Appointment1.API.EndPoints;
using BookAppointmentPortal.Api.Clients;
using Book_An_Appointment1.API.Clients;


namespace Book_An_Appointment1.Services
{
    public class SlotService : BaseApiClient, ISlotService
    {
        private readonly IConfiguration _configuration;

        public SlotService(
            IHttpClientFactory clientFactory,
            ILogger<SlotService> logger,
            ApiUrlBuilder apiUrlBuilder,
            IConfiguration configuration)
            : base(clientFactory, logger,apiUrlBuilder)
        {
            _configuration = configuration;
        }

        public async Task<ApiResponse<DoctorSlotResponse>?> GetSlotsAsync(
            int facilityId, int doctorId, int hospitalLocationId, string fromDate, string toDate)
        {
            

            //var url =
            //    $"{ApiRoutes.GetDoctorSlots}" +
            //    $"?facilityCode={facilityId}" +
            //    $"&doctorRegistrationNo=" +
            //    $"&fromDate={fromDate}" +
            //    $"&toDate={toDate}" +
            //    $"&pageNo=1" +
            //    $"&reservationType=RC" +
            //    $"&doctorId={doctorId}" +
            //    $"&hospitallocationId={hospitalLocationId}";

            var url = ApiUrlBuilder.BuildUrl(ApiRoutes.GetDoctorSlots, "GetDoctorSlotsParams",
                new Dictionary<string, string>
                {
                    { "facilityCode", facilityId.ToString() },
                    { "doctorId", doctorId.ToString() },
                    { "hospitallocationId", hospitalLocationId.ToString() },
                    { "fromDate", fromDate },
                    { "toDate", toDate }
                });
            return await GetAsync<ApiResponse<DoctorSlotResponse>>(url);
        }
    }
}