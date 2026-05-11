using Book_An_Appointment1.Models.Common;
using Book_An_Appointment1.Models.Slot;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;

namespace Book_An_Appointment1.Services
{
    public class SlotService : BaseApiClient, ISlotService
    {
        private readonly IConfiguration _configuration;

        public SlotService(
            IHttpClientFactory clientFactory,
            ILogger<SlotService> logger,
            IConfiguration configuration)
            : base(clientFactory, logger)
        {
            _configuration = configuration;
        }

        public async Task<ApiResponse<DoctorSlotResponse>?> GetSlotsAsync(
            int facilityId,
            int doctorId,
            int hospitalLocationId)
        {
            var fromDate = DateTime.Now.ToString("yyyy-MM-dd");
            var toDate = fromDate;  //DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            var url =
                $"/api/VCP/GetDoctorSlots" +
                $"?facilityCode={facilityId}" +
                $"&doctorRegistrationNo=" +
                $"&fromDate={fromDate}" +
                $"&toDate={toDate}" +
                $"&pageNo=1" +
                $"&reservationType=RC" +
                $"&doctorId={doctorId}" +
                $"&hospitallocationId={hospitalLocationId}";

            return await GetAsync<ApiResponse<DoctorSlotResponse>>(url);
        }
    }
}