using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Models;
using Book_An_Appointment1.Models.Patient;
using Book_An_Appointment1.Services.Interfaces;
using Book_An_Appointment1.ViewModule;
using Book_An_Appointment1.ViewModule.PatientDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace Book_An_Appointment1.Pages.Appointment
{
    public class PatientDetailsModel : PageModel
    {
        private readonly IOtpService _otpService;
        private readonly ILogger<PatientDetailsModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILocationService _locationService;
        private readonly ITitleService _titleService;

        [BindProperty]
        public PatientDetailsViewModel Form { get; set; } = new();

        public AppointmentSessionData? SessionData { get; set; }

        public PatientDetailsModel(
             IOtpService otpService,
             ILogger<PatientDetailsModel> logger,
             IConfiguration configuration,
             ILocationService locationService,
             ITitleService titleService)
        {
            _otpService = otpService;
            _logger = logger;
            _configuration = configuration;
            _locationService = locationService;
            _titleService = titleService;
        }
        public IActionResult OnGet()
        {
            SessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            if (SessionData == null)
                return RedirectToPage("/Appointment/Appointment");

            LoadPageData();
            return Page();
        }
        private void LoadPageData()
        {
            if (SessionData == null) return;

            // Doctor summary
            Form.DoctorName = SessionData.DoctorName;
            Form.DoctorInitials = SessionData.DoctorInitials;
            Form.Specialisation = SessionData.Specialisation;
            Form.Hospital = SessionData.Hospital;
            Form.SlotDisplayText = SessionData.SlotDisplayText;
            Form.ConsultationFee = SessionData.ConsultationFee;

            // Common
            Form.PatientType = SessionData.PatientType;
            Form.AppointmentNotes = SessionData.AppointmentNotes;
            Form.ConsentTerms = SessionData.ConsentAccepted;
            Form.ConsentMarketing = SessionData.ConsentMarketing;

            if (SessionData.PatientType == "registered")
            {
                // Registered patient — pre-fill from session
                Form.RegisteredPatient.Mobile = SessionData.RegisteredPatient.Mobile;
                Form.RegisteredPatient.IsMobileVerified = SessionData.RegisteredPatient.IsMobileVerified;
                Form.RegisteredPatient.GuestPatientId = SessionData.RegisteredPatient.GuestPatientId;
                Form.RegisteredPatient.RegistrationNo = SessionData.RegisteredPatient.RegistrationNo;
                Form.RegisteredPatient.TitleName = SessionData.RegisteredPatient.TitleName;
                Form.RegisteredPatient.TitleId = SessionData.RegisteredPatient.TitleId;
                Form.RegisteredPatient.FirstName = SessionData.RegisteredPatient.FirstName;
                Form.RegisteredPatient.MiddleName = SessionData.RegisteredPatient.MiddleName;
                Form.RegisteredPatient.LastName = SessionData.RegisteredPatient.LastName;
                Form.RegisteredPatient.Gender = SessionData.RegisteredPatient.Gender;
                Form.RegisteredPatient.DOB = SessionData.RegisteredPatient.DOB;
                Form.RegisteredPatient.Email = SessionData.RegisteredPatient.Email;

            }
            else
            {
                // New patient — pre-fill from session
                Form.NewPatient.TitleName = SessionData.NewPatient.TitleName;
                Form.NewPatient.TitleId = SessionData.NewPatient.TitleId;
                Form.NewPatient.FirstName = SessionData.NewPatient.FirstName;
                Form.NewPatient.MiddleName = SessionData.NewPatient.MiddleName;
                Form.NewPatient.LastName = SessionData.NewPatient.LastName;
                Form.NewPatient.DOB = SessionData.NewPatient.DOB;
                Form.NewPatient.Gender = SessionData.NewPatient.Gender;
                Form.NewPatient.Email = SessionData.NewPatient.Email;
                Form.NewPatient.BloodGroup = SessionData.NewPatient.BloodGroup;
                Form.NewPatient.Mobile = SessionData.NewPatient.Mobile;
                Form.NewPatient.IsMobileVerified = SessionData.NewPatient.IsMobileVerified;
                Form.NewPatient.Address = SessionData.NewPatient.Address;
                Form.NewPatient.StateName = SessionData.NewPatient.StateName;
                Form.NewPatient.StateId = SessionData.NewPatient.StateId;
                Form.NewPatient.CityName = SessionData.NewPatient.CityName;
                Form.NewPatient.CityId = SessionData.NewPatient.CityId;
                Form.NewPatient.Pincode = SessionData.NewPatient.Pincode;

                Form.CountryId = SessionData.NewPatient.CountryId > 0
                    ? SessionData.NewPatient.CountryId
                    : _configuration.GetValue<int>("DefaultLocationSettings:DefaultCountryId", 1);

            }

            // Config
            Form.OtpLength = _configuration.GetValue<int>("OtpSettings:OtpLength", 6);
            Form.CountryName = _configuration.GetValue<string>(
                "DefaultLocationSettings:DefaultCountryName", "INDIA") ?? "INDIA";
        }

        public async Task<JsonResult> OnPostSendOtpAsync([FromBody] OtpRequestModel request)
        {

            try
            {
                //var result = await _otpService.SendOtpAsync(request.MobileNo);

                //if (result?.Status?.ToUpper() != "TRUE")
                //{
                //    _logger.LogWarning(
                //        "SendOtp API returned failure | Mobile={Mobile}", request.MobileNo);
                //    return new JsonResult(new
                //    {
                //        success = false,
                //        message = result?.Message ?? "Failed to send OTP"
                //    });
                //}

                return new JsonResult(new
                {
                    success = true,
                    message = "OTP sent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OnPostSendOtpAsync failed | Mobile={Mobile}", request.MobileNo);
                return new JsonResult(new
                {
                    success = false,
                    message = "Something went wrong"
                });
            }
        }

        public async Task<JsonResult> OnPostVerifyOtpAsync([FromBody] VerifyOtpRequestModel request)
        {
            try
            {
                if (request.OtpNo != "123456")
                {
                    _logger.LogWarning(
                        "VerifyOtp failed | Mobile={Mobile}", request.MobileNo);

                    return new JsonResult(new
                    {
                        success = false,
                        message = "Invalid OTP"
                    });
                }

                // Session mein verified mark karo

                var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session)
                  ?? new AppointmentSessionData();

                sessionData.NewPatient.IsMobileVerified = true;
                sessionData.NewPatient.Mobile = request.MobileNo;

                SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

                return new JsonResult(new
                {
                    success = true,
                    message = "OTP verified successfully"
                });


                //var result = await _otpService.VerifyOtpAsync(request.MobileNo, request.OtpNo);
                //if (result?.Status?.ToLower() != "success")
                //{
                //    _logger.LogWarning(
                //        "VerifyOtp failed | Mobile={Mobile}", request.MobileNo);
                //    return new JsonResult(new
                //    {
                //        success = false,
                //        message = result?.Message ?? "Invalid OTP"
                //    });
                //}

                //// Session mein verified mark karo
                //var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session) ?? new AppointmentSessionData();
                //sessionData.NewPatient.IsMobileVerified = true;
                //sessionData.NewPatient.Mobile = request.MobileNo;

                //SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

                //_logger.LogInformation("VerifyOtp success | Mobile={M}", request.MobileNo);
                //return new JsonResult(new 
                //{ 
                //    success = true, 
                //    message = "OTP verified successfully" 
                //});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OnPostVerifyOtpAsync failed | Mobile={Mobile}", request.MobileNo);
                return new JsonResult(new
                {
                    success = false,
                    message = "Something went wrong"
                });
            }
        }
        public async Task<JsonResult> OnGetCountriesAsync()
        {
            try
            {
                var result = await _locationService.GetCountriesAsync();
                var countries = result?.Data?
                    .Where(x => x.CountryId > 0 && !string.IsNullOrWhiteSpace(x.CountryName))
                    .OrderBy(x => x.CountryName)
                    .ToList() ?? new();

                return new JsonResult(new { success = true, data = countries });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCountries failed");
                return new JsonResult(new { success = false, data = new List<object>() });
            }
        }

        public async Task<JsonResult> OnGetStatesAsync(int countryId)
        {
            try
            {
                var result = await _locationService.GetStatesAsync(countryId);
                var states = result?.Data?
                    .Where(x => x.StateId > 0 && !string.IsNullOrWhiteSpace(x.StateName))
                    .OrderBy(x => x.StateName)
                    .ToList() ?? new();

                return new JsonResult(new { success = true, data = states });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStates failed | CountryId={C}", countryId);
                return new JsonResult(new { success = false, data = new List<object>() });
            }
        }
        public async Task<JsonResult> OnGetCitiesAsync(int stateId)
        {
            try
            {
                var result = await _locationService.GetCitiesAsync(stateId);
                var cities = result?.Data?
                    .Where(x => x.CityId > 0 && !string.IsNullOrWhiteSpace(x.CityName))
                    .OrderBy(x => x.CityName)
                    .ToList() ?? new();

                return new JsonResult(new { success = true, data = cities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCities failed | StateId={S}", stateId);
                return new JsonResult(new { success = false, data = new List<object>() });
            }
        }
        public async Task<JsonResult> OnGetTitlesAsync()
        {
            try
            {
                var result = await _titleService.GetTitlesAsync();
                var titles = result?.TitleList?
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .ToList() ?? new();

                return new JsonResult(new { success = true, data = titles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTitles failed");
                return new JsonResult(new { success = false, data = new List<object>() });
            }
        }

        // ── Registered Patient — Send OTP ────────────────────
        public async Task<JsonResult> OnPostSendRegisteredOtpAsync([FromBody] OtpRequestModel request)
        {
            _logger.LogInformation(
                "OnPostSendRegisteredOtpAsync | Mobile={M}", request.MobileNo);
            try
            {
                //var result = await _otpService.SendRegisteredOtpAsync(request.MobileNo);

                //if (result?.Status?.ToUpper() != "TRUE")
                //{
                //    _logger.LogWarning("SendRegisteredOtp failed | Mobile={M}", request.MobileNo);

                //    return new JsonResult(new 
                //    { 
                //        success = false, 
                //        message = result?.Message ?? "Failed to send OTP" 
                //    });
                //}

                return new JsonResult(new { success = true, message = "OTP sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SendRegisteredOtp error | Mobile={M}", request.MobileNo);
                return new JsonResult(new { success = false, message = "Something went wrong" });
            }
        }

        // ── Registered Patient — Verify OTP + Fetch Details ──
        public async Task<JsonResult> OnPostVerifyRegisteredOtpAsync([FromBody] VerifyOtpRequestModel request)
        {
            _logger.LogInformation("OnPostVerifyRegisteredOtpAsync | Mobile={M}", request.MobileNo);
            try
            {
                if (request.OtpNo == "123456")
                {
                    var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session)
                                      ?? new AppointmentSessionData();

                    sessionData.RegisteredPatient.GuestPatientId = "1";
                    sessionData.RegisteredPatient.RegistrationNo = "REG001";
                    sessionData.RegisteredPatient.TitleId = 1;
                    sessionData.RegisteredPatient.TitleName = "Mr.";
                    sessionData.RegisteredPatient.FirstName = "Test";
                    sessionData.RegisteredPatient.MiddleName = "";
                    sessionData.RegisteredPatient.LastName = "Patient";
                    sessionData.RegisteredPatient.Gender = "Male";
                    sessionData.RegisteredPatient.DOB = "01/01/1990";
                    sessionData.RegisteredPatient.Email = "test@test.com";
                    sessionData.RegisteredPatient.Mobile = request.MobileNo;
                    sessionData.RegisteredPatient.IsMobileVerified = true;
                    sessionData.RegisteredPatient.FacilityId = 1;

                    SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

                    return new JsonResult(new
                    {
                        success = true,
                        patient = new
                        {
                            patientName = "Mr. Test Patient",
                            titleName = "Mr.",
                            firstName = "Test",
                            middleName = "",
                            lastName = "Patient",
                            gender = "Male",
                            dob = "01/01/1990",
                            age = "35",
                            emailId = "test@test.com",
                            mobileNo = request.MobileNo,
                            icon = "",
                            isUnRegPat = false
                        }
                    });
                }

                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid OTP"
                });

                //var result = await _otpService.VerifyOtpAsync(request.MobileNo, request.OtpNo);

                //if (result?.Status?.ToLower() != "success")
                //{
                //    _logger.LogWarning("VerifyRegisteredOtp failed | Mobile={M}", request.MobileNo);
                //    return new JsonResult(new { success = false, message = result?.Message ?? "Invalid OTP" });
                //}

                //var patient = result.Data?.PatientList?.FirstOrDefault();

                //if (patient == null)
                //{
                //    _logger.LogWarning("No patient found | Mobile={M}", request.MobileNo);
                //    return new JsonResult(new { success = false, message = "No registered patient found for this mobile number" });
                //}

                //// Session mein registered data save karo
                //var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session)
                //                  ?? new AppointmentSessionData();

                //sessionData.RegisteredPatient.Mobile = patient.MobileNo;
                //sessionData.RegisteredPatient.IsMobileVerified = true;
                //sessionData.RegisteredPatient.GuestPatientId = patient.GuestPatientId;
                //sessionData.RegisteredPatient.RegistrationNo = patient.RegistrationNo;
                //sessionData.RegisteredPatient.TitleId = int.TryParse(patient.TitleId, out var tid) ? tid : 0;
                //sessionData.RegisteredPatient.TitleName = patient.TitleName?.Trim();
                //sessionData.RegisteredPatient.FirstName = patient.FirstName?.Trim() ?? "";
                //sessionData.RegisteredPatient.MiddleName = patient.MiddleName?.Trim();
                //sessionData.RegisteredPatient.LastName = patient.LastName?.Trim() ?? "";
                //sessionData.RegisteredPatient.Gender = patient.Gender;
                //sessionData.RegisteredPatient.DOB = patient.Dob;
                //sessionData.RegisteredPatient.Email = patient.EmailId?.Trim();
                //sessionData.RegisteredPatient.FacilityId = int.TryParse(patient.FacilityId, out var fid) ? fid : 0;

                //SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

                //_logger.LogInformation("RegisteredOtp verified | Patient={Name}", patient.PatientName);

                //return new JsonResult(new
                //{
                //    success = true,
                //    patient = new
                //    {
                //        patientName = patient.PatientName?.Trim(),
                //        titleName = patient.TitleName,
                //        firstName = patient.FirstName?.Trim(),
                //        middleName = patient.MiddleName?.Trim(),
                //        lastName = patient.LastName?.Trim(),
                //        gender = patient.Gender,
                //        dob = patient.Dob,
                //        age = patient.Age,
                //        emailId = patient.EmailId?.Trim(),
                //        mobileNo = patient.MobileNo,
                //        registrationNo = patient.RegistrationNo,
                //        guestPatientId = patient.GuestPatientId,
                //        icon = patient.Icon,
                //        isUnRegPat = patient.IsUnRegPat
                //    }
                //});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "VerifyRegisteredOtp error | Mobile={M}", request.MobileNo);
                return new JsonResult(new { success = false, message = "Something went wrong" });
            }
        }
        // Review & Confirm button
        public IActionResult OnPost()
        {
            _logger.LogInformation("PatientDetails OnPost | PatientType={Type}", Form.PatientType);

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            if (sessionData == null)
                return RedirectToPage("/Appointment/Appointment");

            ModelState.Clear();

            // ── Validation — PatientType ke basis pe ──────
            if (Form.PatientType == "new" || string.IsNullOrEmpty(Form.PatientType))
            {
                ValidateNewPatient();
            }
            else if (Form.PatientType == "registered")
            {
                ValidateRegisteredPatient();
            }

            // ── Common Validation ──────────────────────────
            if (!Form.ConsentTerms)
                ModelState.AddModelError("Form.ConsentTerms", "Please accept terms and conditions");

            // ── Validation Failed ──────────────────────────
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                    foreach (var err in error.Value.Errors)
                        _logger.LogInformation("Key: {Key}, Error: {Error}", error.Key, err.ErrorMessage);

                _logger.LogWarning("PatientDetails validation failed | PatientType={Type}", Form.PatientType);

                RestoreFormForValidationFailure(sessionData);
                return Page();
            }

            // ── Session Save — Common ─────────────────────
            sessionData.PatientType = Form.PatientType;
            sessionData.AppointmentNotes = Form.AppointmentNotes?.Trim();
            sessionData.ConsentAccepted = Form.ConsentTerms;
            sessionData.ConsentMarketing = Form.ConsentMarketing;

            // ── Session Save — Type Specific ──────────────
            if (Form.PatientType == "new" || string.IsNullOrEmpty(Form.PatientType))
            {
                sessionData.NewPatient.TitleName = Form.NewPatient.TitleName?.Trim() ?? "";
                sessionData.NewPatient.TitleId = Form.NewPatient.TitleId;
                sessionData.NewPatient.FirstName = Form.NewPatient.FirstName?.Trim() ?? "";
                sessionData.NewPatient.MiddleName = Form.NewPatient.MiddleName?.Trim();
                sessionData.NewPatient.LastName = Form.NewPatient.LastName?.Trim() ?? "";
                sessionData.NewPatient.DOB = Form.NewPatient.DOB;
                sessionData.NewPatient.Gender = Form.NewPatient.Gender;
                sessionData.NewPatient.Email = Form.NewPatient.Email?.Trim();
                sessionData.NewPatient.BloodGroup = Form.NewPatient.BloodGroup;
                sessionData.NewPatient.Mobile = Form.NewPatient.Mobile?.Trim() ?? "";
                sessionData.NewPatient.CountryCode = Form.NewPatient.CountryCode;
                sessionData.NewPatient.IsMobileVerified = Form.NewPatient.IsMobileVerified;
                sessionData.NewPatient.Address = Form.NewPatient.Address?.Trim();
                sessionData.NewPatient.CountryName = Form.CountryName?.Trim() ?? "";
                sessionData.NewPatient.CountryId = Form.CountryId;
                sessionData.NewPatient.StateName = Form.NewPatient.StateName?.Trim() ?? "";
                sessionData.NewPatient.StateId = Form.NewPatient.StateId ?? 0;
                sessionData.NewPatient.CityName = Form.NewPatient.CityName?.Trim() ?? "";
                sessionData.NewPatient.CityId = Form.NewPatient.CityId ?? 0;
                sessionData.NewPatient.Pincode = Form.NewPatient.Pincode?.Trim();

                // ← Registered ka purana stale data clear karo — kyunki "new" select hua hai
                sessionData.RegisteredPatient = new AppointmentSessionData.RegisteredPatientData();
            }
            else if (Form.PatientType == "registered")
            {
                // Mobile/OTP/Name/Gender/DOB already OnPostVerifyRegisteredOtpAsync mein save hain
                // Yahan kuch save karne ki zaroorat nahi — already session mein hai

                // ← New patient ka purana stale data clear karo
                sessionData.NewPatient = new AppointmentSessionData.NewPatientData();

            }

            SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

            _logger.LogInformation(
                "PatientDetails session saved | PatientType={Type}", sessionData.PatientType);

            return RedirectToPage("/ReviewAndConfirmation/ReviewAndConfirmation");
        }

        // ── Validation Helpers ─────────────────────────────
        private void ValidateNewPatient()
        {
            var p = Form.NewPatient;

            if (string.IsNullOrWhiteSpace(p.TitleName))
                ModelState.AddModelError("Form.NewPatient.TitleName", "Please select a title");

            if (string.IsNullOrWhiteSpace(p.FirstName))
                ModelState.AddModelError("Form.NewPatient.FirstName", "First name is required");
            else if (!Regex.IsMatch(p.FirstName.Trim(), @"^[a-zA-Z\s'\-]+$"))
                ModelState.AddModelError("Form.NewPatient.FirstName", "First name should contain letters only");

            if (!string.IsNullOrWhiteSpace(p.MiddleName) &&
                !Regex.IsMatch(p.MiddleName.Trim(), @"^[a-zA-Z\s'\-]+$"))
                ModelState.AddModelError("Form.NewPatient.MiddleName", "Middle name should contain letters only");

            if (string.IsNullOrWhiteSpace(p.LastName))
                ModelState.AddModelError("Form.NewPatient.LastName", "Last name is required");
            else if (!Regex.IsMatch(p.LastName.Trim(), @"^[a-zA-Z\s'\-]+$"))
                ModelState.AddModelError("Form.NewPatient.LastName", "Last name should contain letters only");

            if (string.IsNullOrWhiteSpace(p.DOB))
                ModelState.AddModelError("Form.NewPatient.DOB", "Date of birth is required");
            else if (DateTime.TryParse(p.DOB, out var dob) && dob >= DateTime.Today)
                ModelState.AddModelError("Form.NewPatient.DOB", "Date of birth cannot be today or a future date");

            if (string.IsNullOrWhiteSpace(p.Gender))
                ModelState.AddModelError("Form.NewPatient.Gender", "Please select gender");

            if (!string.IsNullOrWhiteSpace(p.Email) &&
                !Regex.IsMatch(p.Email.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                ModelState.AddModelError("Form.NewPatient.Email", "Please enter a valid email address");

            if (string.IsNullOrWhiteSpace(p.Mobile))
                ModelState.AddModelError("Form.NewPatient.Mobile", "Mobile number is required");
            else if (!Regex.IsMatch(p.Mobile.Trim(), @"^\d{10}$"))
                ModelState.AddModelError("Form.NewPatient.Mobile", "Please enter a valid 10-digit mobile number");

            if (!p.IsMobileVerified)
                ModelState.AddModelError("Form.NewPatient.IsMobileVerified", "Please verify your mobile number via OTP");

            if (!string.IsNullOrWhiteSpace(p.Pincode) &&
                !Regex.IsMatch(p.Pincode.Trim(), @"^\d{6}$"))
                ModelState.AddModelError("Form.NewPatient.Pincode", "Pincode must be exactly 6 digits");
        }

        private void ValidateRegisteredPatient()
        {
            var p = Form.RegisteredPatient;

            if (string.IsNullOrWhiteSpace(p.Mobile))
                ModelState.AddModelError("Form.RegisteredPatient.Mobile", "Mobile number is required");
            else if (!Regex.IsMatch(p.Mobile.Trim(), @"^\d{10}$"))
                ModelState.AddModelError("Form.RegisteredPatient.Mobile", "Please enter a valid 10-digit mobile number");

            if (!p.IsMobileVerified)
                ModelState.AddModelError("Form.RegisteredPatient.IsMobileVerified", "Please verify your mobile number via OTP");
        }

        private void RestoreFormForValidationFailure(AppointmentSessionData sessionData)
        {
            // Doctor summary
            Form.DoctorName = sessionData.DoctorName;
            Form.DoctorInitials = sessionData.DoctorInitials;
            Form.Specialisation = sessionData.Specialisation;
            Form.Hospital = sessionData.Hospital;
            Form.SlotDisplayText = sessionData.SlotDisplayText;
            Form.ConsultationFee = sessionData.ConsultationFee;

            // Config
            Form.OtpLength = _configuration.GetValue<int>("OtpSettings:OtpLength", 6);
            Form.CountryName = _configuration.GetValue<string>(
                "DefaultLocationSettings:DefaultCountryName", "INDIA") ?? "INDIA";

            if (Form.CountryId <= 0)
                Form.CountryId = _configuration.GetValue<int>("DefaultLocationSettings:DefaultCountryId", 1);

            // Note: Form.NewPatient aur Form.RegisteredPatient already POST data se bhare hain
            // [BindProperty] ne automatically bind kar diya hai — kuch restore karne ki zaroorat nahi
            // Bas State/City names POST mein nahi aate (sirf IDs), agar zaroorat ho to yahan restore karo:
            if (string.IsNullOrEmpty(Form.NewPatient.StateName))
                Form.NewPatient.StateName = sessionData.NewPatient.StateName;
            if (string.IsNullOrEmpty(Form.NewPatient.CityName))
                Form.NewPatient.CityName = sessionData.NewPatient.CityName;
        }
    }
}
