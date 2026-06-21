using Book_An_Appointment1.Models.Patient;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface IOtpService
    {
        Task<OtpResponseModel?> SendOtpAsync(string mobileNo);
        Task<VerifyOtpResponseModel?> VerifyOtpAsync(string mobileNo, string otpNo);
        Task<OtpResponseModel?> SendRegisteredOtpAsync(string mobileNo);
    }
}
