using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Patient;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;

namespace Book_An_Appointment1.Services.Implementations
{
    public class OtpService :BaseApiClient, IOtpService
    {
        public OtpService(IHttpClientFactory clientFactory,ILogger<OtpService> logger,ApiUrlBuilder urlBuilder)
      : base(clientFactory, logger, urlBuilder)
        {
        }

        public async Task<OtpResponseModel?> SendOtpAsync(string mobileNo)
        {
            var response = await PostAsync<OtpResponseModel>(ApiRoutes.GenerateOtp,
               new OtpRequestModel
               {
                   MobileNo = mobileNo,
                   IsFamilyMember = "0"
               });

            return response;
        }

        public async Task<VerifyOtpResponseModel?> VerifyOtpAsync(string mobileNo, string otpNo)
        {
            var response = await PostAsync<VerifyOtpResponseModel>(ApiRoutes.VerifyOtp,
            new VerifyOtpRequestModel
            {
                MobileNo = mobileNo,
                OtpNo = otpNo
            });

           return response;
        }

        public async Task<OtpResponseModel?> SendRegisteredOtpAsync(string mobileNo)
        {

            var response = await PostAsync<OtpResponseModel>(
                ApiRoutes.GenerateOtpForPatient,
                new { mobileNo = mobileNo, isRegistered = "1" });

            return response;
        }
    }
}
