using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Localization;
using Book_An_Appointment1.Models.Appointment;
using Book_An_Appointment1.Services.Interfaces;
using Book_An_Appointment1.ViewModule;
using Book_An_Appointment1.ViewModule.ReviewAndConfirmation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Book_An_Appointment1.Pages.ReviewAndConfimation
{
    public class ReviewAndConfirmationModel : PageModel
    {
        private readonly ILogger<ReviewAndConfirmationModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAppointmentBookingService _bookingService;
        private readonly JsonStringLocalizer _localizer;
        public ReviewAndConfirmationViewModel Summary { get; set; } = new();
        public record CreateAppointmentPostRequest(string? PaymentType);

        public ReviewAndConfirmationModel(ILogger<ReviewAndConfirmationModel> logger, IConfiguration configuration,
            IAppointmentBookingService bookingService, JsonStringLocalizer localizer)
        {
            _localizer = localizer;
            _logger = logger;
            _configuration = configuration;
            _bookingService = bookingService;
        }
        public IActionResult OnGet()
        {
            _logger.LogInformation("ReviewAndConfirmation OnGet started");

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);

            if (sessionData == null)
            {
                _logger.LogWarning("Session null — redirecting to Page 1");
                return RedirectToPage("/Appointment/Appointment");
            }

            // Appointment details
            Summary.HospitalName = sessionData.Hospital ?? "";
            Summary.FacilityId = sessionData.FacilityId ;
            Summary.Specialisation = sessionData.Specialisation ?? "";
            Summary.SpecialisationId = sessionData.SpecialityId;
            Summary.DoctorName = sessionData.DoctorName ?? "";
            Summary.DoctorId = sessionData.DoctorId;
            Summary.SlotDisplayText = sessionData.SlotDisplayText ?? "";
            Summary.SlotDate = sessionData.SlotDate ?? "";
            Summary.SlotTime = sessionData.SlotTime ?? "";
            Summary.ConsultationFee = sessionData.ConsultationFee;
            Summary.RegistrationFee = sessionData.RegistrationFee;
            Summary.PatientType = sessionData.PatientType;

            if (sessionData.PatientType == "registered")
            {

                Summary.FirstName = sessionData.RegisteredPatient.FirstName;
                Summary.MiddleName = sessionData.RegisteredPatient.MiddleName;
                Summary.LastName = sessionData.RegisteredPatient.LastName;
                Summary.Mobile = sessionData.RegisteredPatient.Mobile;
                Summary.Gender = sessionData.RegisteredPatient.Gender;
                Summary.DOB = sessionData.RegisteredPatient.DOB;
                Summary.Title = sessionData.RegisteredPatient.TitleName ?? "";
                Summary.TitleId= sessionData.RegisteredPatient.TitleId;
                Summary.RegistrationNo = sessionData.RegisteredPatient.RegistrationNo;
            }

            else
            {

                Summary.Title = sessionData.NewPatient.TitleName ?? "";
                Summary.TitleId = sessionData.NewPatient.TitleId;
                Summary.FirstName = sessionData.NewPatient.FirstName ?? "";
                Summary.MiddleName = sessionData.NewPatient.MiddleName ?? "";
                Summary.LastName = sessionData.NewPatient.LastName ?? "";
                Summary.Gender = sessionData.NewPatient.Gender ?? "";
                Summary.DOB = sessionData.NewPatient.DOB ?? "";
                Summary.Mobile = sessionData.NewPatient.Mobile ?? "";
                Summary.CountryCode = sessionData.NewPatient.CountryCode ?? "+91";
                Summary.Email = sessionData.NewPatient.Email ?? "";
                Summary.BloodGroup = sessionData.NewPatient.BloodGroup ?? "";
                Summary.Address = sessionData.NewPatient.Address ?? "";
                Summary.CountryId = sessionData.NewPatient.CountryId;
                Summary.StateId = sessionData.NewPatient.StateId;
                Summary.CityId = sessionData.NewPatient.CityId;
                Summary.Pincode = sessionData.NewPatient.Pincode ?? "";

            }

            // Notes + Consent
            Summary.AppointmentNotes = sessionData.AppointmentNotes ?? "";
            Summary.ConsentTerms = sessionData.ConsentAccepted;
            Summary.ConsentMarketing = sessionData.ConsentMarketing;


            Summary.EnablePayNow = _configuration.GetValue<bool>("PaymentSettings:EnablePayNow", true);
            Summary.EnablePayOnArrival = _configuration.GetValue<bool>("PaymentSettings:EnablePayOnArrival", true);

            _logger.LogInformation(
                "ReviewAndConfirmation loaded | Patient={Name} Doctor={Doctor}",
                $"{Summary.FirstName} {Summary.LastName}",
                Summary.DoctorName);

            return Page();
        }

        // ── AJAX: Create Appointment (Pay on arrival / Pay now) ──
        public async Task<JsonResult> OnPostCreateAppointmentAsync([FromBody] CreateAppointmentPostRequest request)
        {
            var paymentType = request?.PaymentType?.Trim().ToLowerInvariant();
            _logger.LogInformation("OnPostCreateAppointmentAsync | PaymentType={P}", paymentType);

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            if (sessionData == null || sessionData.DoctorId <= 0 || string.IsNullOrEmpty(sessionData.SlotTime))
            {
                _logger.LogWarning("CreateAppointment — session expired");
                return new JsonResult(new
                {
                    success = false,
                    message = _localizer.Text("Errors.SessionExpired"),
                    redirectUrl = Url.Page("/Appointment/Appointment")
                });
            }

            var isPayNow = paymentType == "now";
            var isPayOnArrival = paymentType == "arrival";

            // ── Payment option allowed as per config ──────
            if ((!isPayNow && !isPayOnArrival) ||
                (isPayNow && !_configuration.GetValue<bool>("PaymentSettings:EnablePayNow", true)) ||
                (isPayOnArrival && !_configuration.GetValue<bool>("PaymentSettings:EnablePayOnArrival", true)))
            {
                _logger.LogWarning("CreateAppointment — payment option not allowed | PaymentType={P}", paymentType);
                return new JsonResult(new { success = false, message = _localizer.Text("Errors.PaymentOptionUnavailable") });
            }

            // Check gateway url before booking, otherwise appointment is created but user cannot pay
            var gatewayUrl = _configuration["PaymentSettings:PaymentGatewayUrl"]?.Trim();
            if (isPayNow && !Uri.IsWellFormedUriString(gatewayUrl, UriKind.Absolute))
            {
                _logger.LogError("CreateAppointment — PaymentSettings:PaymentGatewayUrl missing or invalid");
                return new JsonResult(new { success = false, message = _localizer.Text("Errors.OnlinePaymentUnavailable") });
            }

            try
            {
                var apiRequest = BuildCreateAppointmentRequest(sessionData, isPayNow);

                // ── Server Side Validation (before API call) ──
                var errors = ValidateBookingRequest(sessionData, apiRequest);
                if (errors.Count > 0)
                {
                    _logger.LogWarning("CreateAppointment validation failed | Errors={E}", string.Join(" | ", errors));
                    return new JsonResult(new { success = false, message = errors[0] });
                }

                var result = await _bookingService.CreateAppointmentAsync(apiRequest);

                var appointmentId = result?.Data?.AppointmentId?.Trim() ?? "";

                if (!string.Equals(result?.Status, "success", StringComparison.OrdinalIgnoreCase) ||
                    !long.TryParse(appointmentId, out var id) || id <= 0)
                {
                    _logger.LogWarning("CreateAppointment API failed | Message={M} ErrorCode={E}",
                        result?.Message, result?.ErrorCode);
                    return new JsonResult(new
                    {
                        success = false,
                        message = !string.IsNullOrWhiteSpace(result?.Message) ? result.Message : _localizer.Text("Errors.BookingFailed")
                    });
                }

                _logger.LogInformation("Appointment created | AppointmentId={A} PaymentType={P}", appointmentId, paymentType);

                // Session data is cleared below, so build the popup fallback first
                var booked = BuildBookedDetailsFromSession(sessionData, appointmentId);

                SessionHelper.ClearAppointmentData(HttpContext.Session);

                if (isPayNow)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        redirectUrl = QueryHelpers.AddQueryString(gatewayUrl!, "appointmentId", appointmentId)
                    });
                }

                await ApplyAppointmentDetailsAsync(booked);

                return new JsonResult(new { success = true, appointment = booked });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnPostCreateAppointmentAsync failed | PaymentType={P}", paymentType);
                return new JsonResult(new { success = false, message = _localizer.Text("Errors.BookingFailed") });
            }
        }

        // ── Private Builders ─────────────────────────────────
        private CreateAppointmentRequest BuildCreateAppointmentRequest(AppointmentSessionData s, bool isPayNow)
        {
            var slotMinutes = _configuration.GetValue<int>("AppointmentApi:DefaultSlotMinutes", 15);
            var from = ParseDate(s.SlotTime);
            var to = ParseDate(HttpContext.Session.GetString("Booking.SlotEnd")) ?? from?.AddMinutes(slotMinutes);

            var request = new CreateAppointmentRequest
            {
                FacilityCode = s.FacilityId.ToString(),
                DoctorId = s.DoctorId.ToString(),
                AppFromDateTime = from?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? s.SlotTime,
                AppToDateTime = to?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "",
                Remarks = s.AppointmentNotes?.Trim() ?? "",
                Source = _configuration["AppointmentApi:Source"] ?? "",
                AppointmentType = _configuration["AppointmentApi:AppointmentType"] ?? "",
                SourceAppointmentId = _configuration["AppointmentApi:SourceAppointmentId"] ?? "",
                IsOnlinePayment = isPayNow
            };

            if (s.PatientType == "registered")
            {
                var p = s.RegisteredPatient;
                request.RegistrationNo = long.TryParse(p.RegistrationNo, out var regNo) ? regNo : 0;
                request.GuestPatientId = string.IsNullOrWhiteSpace(p.GuestPatientId) ? "0" : p.GuestPatientId;
                request.TitleId = p.TitleId.ToString();
                request.FirstName = p.FirstName?.Trim() ?? "";
                request.MiddleName = p.MiddleName?.Trim() ?? "";
                request.LastName = p.LastName?.Trim() ?? "";
                request.Dob = FormatDob(p.DOB);
                request.Gender = GetGenderCode(p.Gender);
                request.MobileNo = p.Mobile?.Trim() ?? "";
            }
            else
            {
                var p = s.NewPatient;
                request.RegistrationNo = 0;
                request.GuestPatientId = "0";
                request.TitleId = p.TitleId.ToString();
                request.FirstName = p.FirstName?.Trim() ?? "";
                request.MiddleName = p.MiddleName?.Trim() ?? "";
                request.LastName = p.LastName?.Trim() ?? "";
                request.Dob = FormatDob(p.DOB);
                request.Gender = GetGenderCode(p.Gender);
                request.MobileNo = p.Mobile?.Trim() ?? "";
                request.Address1 = p.Address?.Trim() ?? "";
                request.CityCode = p.CityId > 0 ? p.CityId.ToString() : "";
                request.StateCode = p.StateId > 0 ? p.StateId.ToString() : "";
                request.CountryCode = p.CountryId > 0 ? p.CountryId.ToString() : "";
                request.PinCode = p.Pincode?.Trim() ?? "";
            }

            return request;
        }

        // ── Validation Helpers ─────────────────────────────
        private List<string> ValidateBookingRequest(AppointmentSessionData s, CreateAppointmentRequest r)
        {
            var errors = new List<string>();
            var isRegistered = s.PatientType == "registered";

            // Config — wrong setup should never reach the API
            if (string.IsNullOrWhiteSpace(r.Source) || string.IsNullOrWhiteSpace(r.AppointmentType))
            {
                _logger.LogError("AppointmentApi:Source / AppointmentType missing in appsettings");
                errors.Add(_localizer.Text("Errors.BookingUnavailable"));
                return errors;
            }

            // Appointment
            if (s.FacilityId <= 0 || s.DoctorId <= 0)
                errors.Add(_localizer.Text("Errors.SelectDoctorAgain"));

            var from = ParseDate(r.AppFromDateTime);
            var to = ParseDate(r.AppToDateTime);
            if (from == null || to == null || to <= from)
                errors.Add(_localizer.Text("Errors.InvalidSlot"));
            else if (from <= DateTime.Now)
                errors.Add(_localizer.Text("Errors.SlotPassed"));

            // Patient
            if (isRegistered ? !s.RegisteredPatient.IsMobileVerified : !s.NewPatient.IsMobileVerified)
                errors.Add(_localizer.Text("Validation.VerifyMobile"));

            if (!Regex.IsMatch(r.MobileNo, @"^\d{10}$"))
                errors.Add(_localizer.Text("Validation.MobileInvalid"));

            if (isRegistered && r.RegistrationNo <= 0)
                errors.Add(_localizer.Text("Errors.RegisteredNotFound"));

            if (!isRegistered)
            {
                if (r.TitleId == "0" || string.IsNullOrWhiteSpace(r.TitleId))
                    errors.Add(_localizer.Text("Validation.SelectTitle"));
                if (string.IsNullOrWhiteSpace(r.LastName))
                    errors.Add(_localizer.Text("Validation.LastNameRequired"));
            }

            if (string.IsNullOrWhiteSpace(r.FirstName))
                errors.Add(_localizer.Text("Validation.FirstNameRequired"));

            var dob = ParseDate(r.Dob);
            if (dob == null)
                errors.Add(_localizer.Text("Validation.DobRequired"));
            else if (dob >= DateTime.Today)
                errors.Add(_localizer.Text("Validation.DobPast"));

            if (string.IsNullOrWhiteSpace(r.Gender))
                errors.Add(_localizer.Text("Validation.SelectGender"));

            if (!s.ConsentAccepted)
                errors.Add(_localizer.Text("Validation.AcceptTerms"));

            return errors;
        }

        private BookedAppointmentViewModel BuildBookedDetailsFromSession(AppointmentSessionData s, string appointmentId)
        {
            var isRegistered = s.PatientType == "registered";
            var slot = ParseDate(s.SlotTime);
            var total = s.ConsultationFee + (isRegistered ? 0 : s.RegistrationFee);

            return new BookedAppointmentViewModel
            {
                AppointmentId = appointmentId,
                PatientName = CleanSpaces(isRegistered
                    ? $"{s.RegisteredPatient.TitleName} {s.RegisteredPatient.FirstName} {s.RegisteredPatient.MiddleName} {s.RegisteredPatient.LastName}"
                    : $"{s.NewPatient.TitleName} {s.NewPatient.FirstName} {s.NewPatient.MiddleName} {s.NewPatient.LastName}"),
                RegistrationNo = isRegistered ? s.RegisteredPatient.RegistrationNo : null,
                Mobile = isRegistered ? s.RegisteredPatient.Mobile : s.NewPatient.Mobile,
                DoctorName = s.DoctorName,
                Specialisation = s.Specialisation,
                HospitalName = s.Hospital,
                AppointmentDate = slot?.ToString("dd MMM yyyy") ?? s.SlotDate,
                AppointmentDay = slot?.ToString("dddd") ?? "",
                AppointmentTime = slot?.ToString("hh:mm tt") ?? s.SlotTime,
                AmountPayable = "₹ " + Money(total),
                PaymentMode = _localizer.Text("Review.PayAtHospital")
            };
        }

        // Booked details from GetAppointmentByID; on any failure keep the session values
        // because the appointment itself is already created
        private async Task ApplyAppointmentDetailsAsync(BookedAppointmentViewModel booked)
        {
            try
            {
                var result = await _bookingService.GetAppointmentByIdAsync(booked.AppointmentId);
                var item = result?.Data?.FirstOrDefault();

                if (!string.Equals(result?.Status, "success", StringComparison.OrdinalIgnoreCase) || item == null)
                {
                    _logger.LogWarning("GetAppointmentByID no record | AppointmentId={A} Message={M}",
                        booked.AppointmentId, result?.Message);
                    return;
                }

                var start = ParseDate(item.AppStartTime);

                booked.PatientName = Prefer(item.PatientName, booked.PatientName);
                booked.RegistrationNo = Prefer(item.RegistrationNo, booked.RegistrationNo ?? "");
                booked.DoctorName = Prefer(item.DoctorName, booked.DoctorName);
                booked.Specialisation = Prefer(item.SpecializationName, booked.Specialisation);
                booked.HospitalName = Prefer(item.FacilityName, booked.HospitalName);
                booked.AppointmentDate = start?.ToString("dd MMM yyyy") ?? booked.AppointmentDate;
                booked.AppointmentDay = start?.ToString("dddd") ?? booked.AppointmentDay;
                booked.AppointmentTime = start?.ToString("hh:mm tt") ?? booked.AppointmentTime;
                booked.VisitType = item.VisitType;
                booked.Status = item.Status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAppointmentByID failed | AppointmentId={A}", booked.AppointmentId);
            }
        }

        // ── Helpers ──────────────────────────────────────────
        private string GetGenderCode(string? gender)
        {
            var g = gender?.Trim();
            if (string.IsNullOrEmpty(g)) return "";
            if (int.TryParse(g, out _)) return g;   // already a code

            // Match "Male" / "M" against AppointmentApi:GenderCodes keys
            var match = _configuration.GetSection("AppointmentApi:GenderCodes").GetChildren()
                .FirstOrDefault(x => string.Equals(x.Key, g, StringComparison.OrdinalIgnoreCase) ||
                                     (g.Length == 1 && x.Key.StartsWith(g, StringComparison.OrdinalIgnoreCase)));

            if (match == null)
                _logger.LogWarning("Gender code not configured | Gender={G}", g);

            return match?.Value ?? "";
        }

        private static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

        private static string FormatDob(string? dob) =>
            ParseDate(dob)?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? dob ?? "";

        private static string Prefer(string? apiValue, string fallback) =>
            CleanSpaces(string.IsNullOrWhiteSpace(apiValue) ? fallback : apiValue);

        private static string CleanSpaces(string value) =>
            string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        private static string Money(decimal v) => v == decimal.Truncate(v) ? v.ToString("N0") : v.ToString("N2");
    }
}
