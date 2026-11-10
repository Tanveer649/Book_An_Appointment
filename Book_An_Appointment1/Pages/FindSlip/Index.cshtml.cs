using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Localization;
using Book_An_Appointment1.Models.Appointment;
using Book_An_Appointment1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace Book_An_Appointment1.Pages.FindSlip
{
    // "Find my slip": /find-slip — mobile → OTP → the appointment slips issued for that mobile (PDF / slip page).
    // appsettings FindSlipSettings:Enabled. Slips are remembered at booking time (Services/Implementations/SlipIndex.cs).
    public class IndexModel : PageModel
    {
        private const string VerifiedMobileKey = "FindSlip.Mobile";
        private const string TestOtp = "123456";            // Development only, like the booking pages

        private readonly IOtpService _otpService;
        private readonly ISlipIndex _slipIndex;
        private readonly IAppointmentSlipService _slipService;
        private readonly IConfiguration _configuration;
        private readonly JsonStringLocalizer _localizer;
        private readonly ILogger<IndexModel> _logger;
        private readonly bool _useTestOtp;

        public IndexModel(IOtpService otpService, ISlipIndex slipIndex, IAppointmentSlipService slipService,
            IConfiguration configuration, JsonStringLocalizer localizer, IWebHostEnvironment environment, ILogger<IndexModel> logger)
        {
            _otpService = otpService;
            _slipIndex = slipIndex;
            _slipService = slipService;
            _configuration = configuration;
            _localizer = localizer;
            _logger = logger;
            _useTestOtp = environment.IsDevelopment();
        }

        public record MobileRequest(string? MobileNo);
        public record VerifyRequest(string? MobileNo, string? OtpNo);

        public int OtpLength => _configuration.GetValue("OtpSettings:OtpLength", 6);
        private bool Enabled => _configuration.GetValue("FindSlipSettings:Enabled", false);

        public IActionResult OnGet()
        {
            if (!Enabled) return NotFound();
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers["X-Robots-Tag"] = "noindex";
            return Page();
        }

        // Send OTP: registered-patient OTP first; numbers the HIS does not know as registered get the signup OTP
        public async Task<JsonResult> OnPostFindSlipSendOtpAsync([FromBody] MobileRequest request)
        {
            if (!Enabled) return Fail(_localizer.Text("Errors.SomethingWrong"));
            var mobile = request?.MobileNo?.Trim() ?? "";
            if (!Regex.IsMatch(mobile, @"^\d{10}$")) return Fail(_localizer.Text("Validation.MobileInvalid"));

            HttpContext.Session.Remove(VerifiedMobileKey);
            if (_useTestOtp) return new JsonResult(new { success = true });

            try
            {
                var result = await _otpService.SendRegisteredOtpAsync(mobile);
                if (result?.Status?.ToUpper() != "TRUE")
                    result = await _otpService.SendOtpAsync(mobile);
                if (result?.Status?.ToUpper() != "TRUE")
                {
                    _logger.LogWarning("Find my slip — OTP not sent | Mobile={M} Message={Msg}", LogMask.Mobile(mobile), result?.Message);
                    return Fail(!string.IsNullOrWhiteSpace(result?.Message) ? result.Message : _localizer.Text("Errors.OtpSendFailed"));
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Find my slip — OTP send error | Mobile={M}", LogMask.Mobile(mobile));
                return Fail(_localizer.Text("Errors.SomethingWrong"));
            }
        }

        // Verify OTP → the slips issued for this mobile, each with fresh signed links
        public async Task<JsonResult> OnPostFindSlipVerifyAsync([FromBody] VerifyRequest request)
        {
            if (!Enabled) return Fail(_localizer.Text("Errors.SomethingWrong"));
            var mobile = request?.MobileNo?.Trim() ?? "";
            var otp = request?.OtpNo?.Trim() ?? "";
            if (!Regex.IsMatch(mobile, @"^\d{10}$") || !Regex.IsMatch(otp, $@"^\d{{{OtpLength}}}$"))
                return Fail(_localizer.Text("Errors.InvalidOtp"));

            try
            {
                var verified = _useTestOtp
                    ? otp == TestOtp
                    : string.Equals((await _otpService.VerifyOtpAsync(mobile, otp))?.Status, "success", StringComparison.OrdinalIgnoreCase);
                if (!verified)
                {
                    _logger.LogWarning("Find my slip — wrong OTP | Mobile={M}", LogMask.Mobile(mobile));
                    return Fail(_localizer.Text("Errors.InvalidOtpRetry"));
                }

                HttpContext.Session.SetString(VerifiedMobileKey, mobile);
                // Patient name + UHID tell family members on one mobile which slip is whose: live from the hospital API
                // (GetAppointmentByID, cached briefly) when it has them, otherwise from the booking record.
                var records = _slipIndex.FindByMobile(mobile);
                var tickets = records.Select(r => new SlipTicket(r.AppointmentId, r.Amount, mobile, r.PaymentMode, r.Uhid, r.PatientName)).ToList();
                var details = await Task.WhenAll(tickets.Select(t => _slipService.GetSlipAsync(t)));

                var slips = records.Select((r, i) =>
                {
                    var url = _slipService.SlipUrl(Request, _slipService.CreateToken(tickets[i]));
                    var live = details[i];
                    return new
                    {
                        appointmentId = r.AppointmentId,
                        patientName = !string.IsNullOrWhiteSpace(live?.PatientName) ? live!.PatientName : r.PatientName,
                        uhid = !string.IsNullOrWhiteSpace(live?.RegistrationNo) ? live!.RegistrationNo : r.Uhid,
                        doctorName = !string.IsNullOrWhiteSpace(live?.DoctorName) ? live!.DoctorName : r.DoctorName,
                        specialisation = !string.IsNullOrWhiteSpace(live?.Specialisation) ? live!.Specialisation : r.Specialisation,
                        date = r.StartsAt?.ToString("ddd, d MMM yyyy") ?? "",
                        time = r.StartsAt?.ToString("hh:mm tt") ?? "",
                        upcoming = r.StartsAt == null || r.StartsAt >= DateTime.Now,
                        slipUrl = url,
                        pdfUrl = url + "?handler=Pdf"
                    };
                }).ToList();

                _logger.LogInformation("Find my slip — {N} slip(s) found | Mobile={M}", slips.Count, LogMask.Mobile(mobile));
                return new JsonResult(new { success = true, slips });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Find my slip — verify error | Mobile={M}", LogMask.Mobile(mobile));
                return Fail(_localizer.Text("Errors.SomethingWrong"));
            }
        }

        private static JsonResult Fail(string message) => new(new { success = false, message });
    }
}
