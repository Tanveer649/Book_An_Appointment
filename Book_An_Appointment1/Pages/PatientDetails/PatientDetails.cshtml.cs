using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Localization;
using Book_An_Appointment1.Models;
using Book_An_Appointment1.Models.Patient;
using Book_An_Appointment1.Services.Interfaces;
using Book_An_Appointment1.ViewModule;
using Book_An_Appointment1.ViewModule.PatientDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
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
        private readonly JsonStringLocalizer _localizer;

        [BindProperty]
        public PatientDetailsViewModel Form { get; set; } = new();

        public AppointmentSessionData? SessionData { get; set; }

        // Family members linked to the verified mobile — rendered as cards (restored on Back / validation failure)
        public List<RegisteredPatientInfo> RegisteredPatients { get; set; } = new();
        public record SelectRegisteredPatientRequest(string? RegistrationNo);

        public PatientDetailsModel(
             IOtpService otpService,
             ILogger<PatientDetailsModel> logger,
             IConfiguration configuration,
             ILocationService locationService,
             ITitleService titleService,
             JsonStringLocalizer localizer)
        {
            _localizer = localizer;
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
            Form.SlotDisplayText = AppointmentModel.FormatSlotDisplay(SessionData.SlotTime);
            Form.ConsultationFee = SessionData.ConsultationFee;
            Form.RegistrationFee = SessionData.RegistrationFee;

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

                if (SessionData.RegisteredPatient.IsMobileVerified)
                    RegisteredPatients = SessionData.RegisteredPatient.PatientList;

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
                    message = _localizer.Text("Errors.SomethingWrong")
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
                        message = _localizer.Text("Errors.InvalidOtp")
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
                //        message = result?.Message ?? _localizer.Text("Errors.InvalidOtp")
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
                    message = _localizer.Text("Errors.SomethingWrong")
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
                return new JsonResult(new { success = false, message = _localizer.Text("Errors.SomethingWrong") });
            }
        }

        // ── Registered Patient — Verify OTP + Fetch Details ──
        public async Task<JsonResult> OnPostVerifyRegisteredOtpAsync([FromBody] VerifyOtpRequestModel request)
        {

            _logger.LogInformation("OnPostVerifyRegisteredOtpAsync | Mobile={M}", request.MobileNo);

            try
            {
                VerifyOtpResponseData? apiData;

                // TESTING: hardcoded OTP response
                if (request.OtpNo == "123456")
                {
                    apiData = new VerifyOtpResponseData
                    {
                        MobileNo = request.MobileNo,
                        Otp = request.OtpNo,
                        IsUserExist = true,
                        Id = 2345434,
                        Name = "Test1",
                        Gender = "1",
                        Dob = "1/31/1995 12:00:00 AM",
                        EmailId = "wdvctwve",
                        IsHisRegistered = true,
                        VerifyPatinetListCount = "4",

                        // One mobile — whole family registered (self, wife, daughter, son)
                        PatientList = new List<RegisteredPatientInfo>
                        {
                            new RegisteredPatientInfo
                            {
                                HospitalLocationId = "1",
                                FacilityID = "1",
                                RegistrationId = 30023044,
                                RegistrationNo = "567826738",
                                EncounterId = "0",
                                EncounterNo = "0",
                                PatientName = "Mr. Test1  Test1",
                                Gender = "Male",
                                Dob = "1/31/1995 12:00:00 AM",
                                Age = "31 Yrs/Male",
                                EmailId = "wdvctwve",
                                MobileNo = request.MobileNo,
                                PatientType = "",
                                IsUnRegPat = false,
                                TcFacilityId = null
                            },
                            new RegisteredPatientInfo
                            {
                                HospitalLocationId = "1",
                                FacilityID = "1",
                                RegistrationId = 30023045,
                                RegistrationNo = "567826739",
                                EncounterId = "0",
                                EncounterNo = "0",
                                PatientName = "Mrs. Sunita  Sharma",
                                Gender = "Female",
                                Dob = "8/14/1997 12:00:00 AM",
                                Age = "29 Yrs/Female",
                                EmailId = "sunita.sharma@example.com",
                                MobileNo = request.MobileNo,
                                PatientType = "",
                                IsUnRegPat = false,
                                TcFacilityId = null
                            },
                            new RegisteredPatientInfo
                            {
                                HospitalLocationId = "1",
                                FacilityID = "1",
                                RegistrationId = 30023046,
                                RegistrationNo = "567826740",
                                EncounterId = "0",
                                EncounterNo = "0",
                                PatientName = "Ms. Priya  Sharma",
                                Gender = "Female",
                                Dob = "3/5/2019 12:00:00 AM",
                                Age = "7 Yrs/Female",
                                EmailId = "",
                                MobileNo = request.MobileNo,
                                PatientType = "",
                                IsUnRegPat = false,
                                TcFacilityId = null
                            },
                            new RegisteredPatientInfo
                            {
                                HospitalLocationId = "1",
                                FacilityID = "1",
                                RegistrationId = 30023047,
                                RegistrationNo = "567826741",
                                EncounterId = "0",
                                EncounterNo = "0",
                                PatientName = "Master Aarav  Sharma",
                                Gender = "Male",
                                Dob = "11/22/2022 12:00:00 AM",
                                Age = "3 Yrs/Male",
                                EmailId = "",
                                MobileNo = request.MobileNo,
                                PatientType = "",
                                IsUnRegPat = false,
                                TcFacilityId = null
                            }
                        }
                    };
                }
                else
                {
                    // REAL OTP API
                    var result = await _otpService.VerifyOtpAsync(request.MobileNo, request.OtpNo);

                    if (result?.Status?.ToLower() != "success")
                    {
                        _logger.LogWarning("VerifyRegisteredOtp failed | Mobile={M}", request.MobileNo);

                        return new JsonResult(new
                        {
                            success = false,
                            message = result?.Message ?? _localizer.Text("Errors.InvalidOtp")
                        });
                    }

                    apiData = result.Data;
                }

                if (apiData == null)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = _localizer.Text("Errors.UnableToVerifyOtp")
                    });
                }

                var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session)
                                  ?? new AppointmentSessionData();

                var registeredPatient = sessionData.RegisteredPatient;

                // Top-level API response details
                registeredPatient.Mobile = apiData.MobileNo ?? request.MobileNo;
                registeredPatient.IsMobileVerified = true;

                registeredPatient.Otp = apiData.Otp;
                registeredPatient.IsUserExist = apiData.IsUserExist;
                registeredPatient.Id = apiData.Id;
                registeredPatient.Name = apiData.Name;
                registeredPatient.ApiResponseGender = apiData.Gender;
                registeredPatient.ApiResponseDob = apiData.Dob;
                registeredPatient.EmailId = apiData.EmailId;
                registeredPatient.IsHisRegistered = apiData.IsHisRegistered;
                registeredPatient.VerifyPatinetListCount =  apiData.VerifyPatinetListCount;

                // Store ALL family members returned for this mobile number
                registeredPatient.PatientList = apiData.PatientList ?? new List<RegisteredPatientInfo>();

                // Only one member → select automatically; more than one → user picks a card
                // (OnPostSelectRegisteredPatientAsync). Previous selection never carries over to a new OTP.
                var selectedPatient = registeredPatient.PatientList.Count == 1
                    ? registeredPatient.PatientList[0]
                    : null;

                if (selectedPatient != null)
                    ApplySelectedPatient(registeredPatient, selectedPatient);
                else
                    ClearSelectedPatient(registeredPatient);

                SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

                return new JsonResult(new
                {
                    success = true,

                    // Top-level API details
                    mobileNo = apiData.MobileNo,
                    otp = apiData.Otp,
                    isUserExist = apiData.IsUserExist,
                    id = apiData.Id,
                    name = apiData.Name,
                    gender = apiData.Gender,
                    dob = apiData.Dob,
                    emailId = apiData.EmailId,
                    isHisRegistered = apiData.IsHisRegistered,
                    verifyPatinetListCount = apiData.VerifyPatinetListCount,

                    // Return all family members
                    patientList = registeredPatient.PatientList,

                    // Currently selected patient (null when user has to choose)
                    patient = selectedPatient,
                    selected = selectedPatient != null ? SelectedPatientFields(registeredPatient) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "OnPostVerifyRegisteredOtpAsync failed | Mobile={M}",
                    request.MobileNo);

                return new JsonResult(new
                {
                    success = false,
                    message = _localizer.Text("Errors.OtpVerifyError")
                });
            }
        }
        // ── Registered Patient — Select family member card ──
        public JsonResult OnPostSelectRegisteredPatient([FromBody] SelectRegisteredPatientRequest request)
        {
            _logger.LogInformation("OnPostSelectRegisteredPatient | RegistrationNo={R}", request?.RegistrationNo);

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            var registeredPatient = sessionData?.RegisteredPatient;

            if (registeredPatient == null || !registeredPatient.IsMobileVerified)
                return new JsonResult(new { success = false, message = _localizer.Text("Validation.VerifyMobile") });

            // Only a member returned for this verified mobile can be selected
            var patient = registeredPatient.PatientList.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.RegistrationNo) && x.RegistrationNo == request?.RegistrationNo?.Trim());

            if (patient == null)
            {
                _logger.LogWarning("SelectRegisteredPatient — not in patient list | RegistrationNo={R}", request?.RegistrationNo);
                return new JsonResult(new { success = false, message = _localizer.Text("Errors.PatientNotFound") });
            }

            ApplySelectedPatient(registeredPatient, patient);
            SessionHelper.SetAppointmentData(HttpContext.Session, sessionData!);

            return new JsonResult(new { success = true, selected = SelectedPatientFields(registeredPatient) });
        }

        private static void ApplySelectedPatient(AppointmentSessionData.RegisteredPatientData registeredPatient, RegisteredPatientInfo selectedPatient)
        {
            // Per-patient value from the API ("0" for HIS-registered patients)
            registeredPatient.GuestPatientId = string.IsNullOrWhiteSpace(selectedPatient.GuestPatientId)
                ? "0"
                : selectedPatient.GuestPatientId.Trim();
            registeredPatient.RegistrationId = selectedPatient.RegistrationId;
            registeredPatient.RegistrationNo = selectedPatient.RegistrationNo;

            registeredPatient.EncounterId = selectedPatient.EncounterId;
            registeredPatient.EncounterNo = selectedPatient.EncounterNo;
            registeredPatient.HospitalLocationId = selectedPatient.HospitalLocationId;

            registeredPatient.FacilityId =
                int.TryParse(selectedPatient.FacilityID, out var facilityId)
                    ? facilityId
                    : 0;

            registeredPatient.PatientName = selectedPatient.PatientName;
            registeredPatient.Gender = selectedPatient.Gender ?? "";
            registeredPatient.DOB = selectedPatient.Dob ?? "";
            registeredPatient.Age = selectedPatient.Age;
            registeredPatient.Email = selectedPatient.EmailId;

            registeredPatient.PatientType = selectedPatient.PatientType;
            registeredPatient.IsUnRegPat = selectedPatient.IsUnRegPat;
            registeredPatient.TcFacilityId = selectedPatient.TcFacilityId;

            // Name parts from the API; older responses only had patientName → keep it whole in FirstName
            if (!string.IsNullOrWhiteSpace(selectedPatient.FirstName))
            {
                registeredPatient.TitleName = selectedPatient.TitleName?.Trim();
                registeredPatient.TitleId = int.TryParse(selectedPatient.TitleId, out var titleId) ? titleId : 0;
                registeredPatient.FirstName = selectedPatient.FirstName.Trim();
                registeredPatient.MiddleName = selectedPatient.MiddleName?.Trim() ?? "";
                registeredPatient.LastName = selectedPatient.LastName?.Trim() ?? "";
            }
            else
            {
                registeredPatient.TitleName = null;
                registeredPatient.TitleId = 0;
                registeredPatient.FirstName = selectedPatient.PatientName ?? "";
                registeredPatient.MiddleName = "";
                registeredPatient.LastName = "";
            }
        }

        private static void ClearSelectedPatient(AppointmentSessionData.RegisteredPatientData registeredPatient)
        {
            registeredPatient.GuestPatientId = null;
            registeredPatient.RegistrationId = 0;
            registeredPatient.RegistrationNo = null;
            registeredPatient.EncounterId = null;
            registeredPatient.EncounterNo = null;
            registeredPatient.HospitalLocationId = null;
            registeredPatient.FacilityId = 0;
            registeredPatient.PatientName = null;
            registeredPatient.Gender = "";
            registeredPatient.DOB = "";
            registeredPatient.Age = null;
            registeredPatient.Email = null;
            registeredPatient.PatientType = null;
            registeredPatient.IsUnRegPat = false;
            registeredPatient.TcFacilityId = null;
            registeredPatient.TitleName = null;
            registeredPatient.TitleId = 0;
            registeredPatient.FirstName = "";
            registeredPatient.MiddleName = "";
            registeredPatient.LastName = "";
        }

        // Values for the registered hidden inputs (Reg_*)
        private static object SelectedPatientFields(AppointmentSessionData.RegisteredPatientData p) => new
        {
            guestPatientId = p.GuestPatientId,
            registrationNo = p.RegistrationNo,
            titleName = p.TitleName,
            titleId = p.TitleId,
            firstName = p.FirstName,
            middleName = p.MiddleName,
            lastName = p.LastName,
            gender = p.Gender,
            dob = p.DOB,
            emailId = p.Email
        };

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
                ValidateRegisteredPatient(sessionData);
            }

            // ── Common Validation ──────────────────────────
            if (!Form.ConsentTerms)
                ModelState.AddModelError("Form.ConsentTerms", _localizer.Text("Validation.AcceptTerms"));

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
                ModelState.AddModelError("Form.NewPatient.TitleName", _localizer.Text("Validation.SelectTitle"));

            if (string.IsNullOrWhiteSpace(p.FirstName))
                ModelState.AddModelError("Form.NewPatient.FirstName", _localizer.Text("Validation.FirstNameRequired"));
            else if (!Regex.IsMatch(p.FirstName.Trim(), @"^[a-zA-Z\s'\-]+$"))
                ModelState.AddModelError("Form.NewPatient.FirstName", _localizer.Text("Validation.FirstNameLetters"));

            if (!string.IsNullOrWhiteSpace(p.MiddleName) &&
                !Regex.IsMatch(p.MiddleName.Trim(), @"^[a-zA-Z\s'\-]+$"))
                ModelState.AddModelError("Form.NewPatient.MiddleName", _localizer.Text("Validation.MiddleNameLetters"));

            if (string.IsNullOrWhiteSpace(p.LastName))
                ModelState.AddModelError("Form.NewPatient.LastName", _localizer.Text("Validation.LastNameRequired"));
            else if (!Regex.IsMatch(p.LastName.Trim(), @"^[a-zA-Z\s'\-]+$"))
                ModelState.AddModelError("Form.NewPatient.LastName", _localizer.Text("Validation.LastNameLetters"));

            if (string.IsNullOrWhiteSpace(p.DOB))
                ModelState.AddModelError("Form.NewPatient.DOB", _localizer.Text("Validation.DobRequired"));
            else if (DateTime.TryParse(p.DOB, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob) && dob >= DateTime.Today)
                ModelState.AddModelError("Form.NewPatient.DOB", _localizer.Text("Validation.DobPast"));

            if (string.IsNullOrWhiteSpace(p.Gender))
                ModelState.AddModelError("Form.NewPatient.Gender", _localizer.Text("Validation.SelectGender"));

            if (!string.IsNullOrWhiteSpace(p.Email) &&
                !Regex.IsMatch(p.Email.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                ModelState.AddModelError("Form.NewPatient.Email", _localizer.Text("Validation.EmailInvalid"));

            if (string.IsNullOrWhiteSpace(p.Mobile))
                ModelState.AddModelError("Form.NewPatient.Mobile", _localizer.Text("Validation.MobileRequired"));
            else if (!Regex.IsMatch(p.Mobile.Trim(), @"^\d{10}$"))
                ModelState.AddModelError("Form.NewPatient.Mobile", _localizer.Text("Validation.MobileInvalid"));

            if (!p.IsMobileVerified)
                ModelState.AddModelError("Form.NewPatient.IsMobileVerified", _localizer.Text("Validation.VerifyMobile"));

            if (!string.IsNullOrWhiteSpace(p.Pincode) &&
                !Regex.IsMatch(p.Pincode.Trim(), @"^\d{6}$"))
                ModelState.AddModelError("Form.NewPatient.Pincode", _localizer.Text("Validation.PincodeInvalid"));
        }

        private void ValidateRegisteredPatient(AppointmentSessionData sessionData)
        {
            var p = Form.RegisteredPatient;
            var saved = sessionData.RegisteredPatient;   // OTP + selection are trusted from session, not hidden inputs

            if (string.IsNullOrWhiteSpace(p.Mobile))
                ModelState.AddModelError("Form.RegisteredPatient.Mobile", _localizer.Text("Validation.MobileRequired"));
            else if (!Regex.IsMatch(p.Mobile.Trim(), @"^\d{10}$"))
                ModelState.AddModelError("Form.RegisteredPatient.Mobile", _localizer.Text("Validation.MobileInvalid"));

            if (!p.IsMobileVerified || !saved.IsMobileVerified || saved.Mobile != p.Mobile?.Trim())
                ModelState.AddModelError("Form.RegisteredPatient.IsMobileVerified", _localizer.Text("Validation.VerifyMobile"));
            else if (string.IsNullOrWhiteSpace(saved.RegistrationNo) ||
                     !saved.PatientList.Any(x => x.RegistrationNo == saved.RegistrationNo))
                ModelState.AddModelError("Form.RegisteredPatient.RegistrationNo", _localizer.Text("Validation.SelectPatient"));
        }

        private void RestoreFormForValidationFailure(AppointmentSessionData sessionData)
        {
            // Doctor summary
            Form.DoctorName = sessionData.DoctorName;
            Form.DoctorInitials = sessionData.DoctorInitials;
            Form.Specialisation = sessionData.Specialisation;
            Form.Hospital = sessionData.Hospital;
            Form.SlotDisplayText = AppointmentModel.FormatSlotDisplay(sessionData.SlotTime);
            Form.ConsultationFee = sessionData.ConsultationFee;
            Form.RegistrationFee = sessionData.RegistrationFee;

            // Config
            Form.OtpLength = _configuration.GetValue<int>("OtpSettings:OtpLength", 6);
            Form.CountryName = _configuration.GetValue<string>(
                "DefaultLocationSettings:DefaultCountryName", "INDIA") ?? "INDIA";

            if (Form.CountryId <= 0)
                Form.CountryId = _configuration.GetValue<int>("DefaultLocationSettings:DefaultCountryId", 1);

            // Note: Form.NewPatient aur Form.RegisteredPatient already POST data se bhare hain
            // [BindProperty] ne automatically bind kar diya hai — kuch restore karne ki zaroorat nahi
            // Bas State/City names POST mein nahi aate (sirf IDs), agar zaroorat ho to yahan restore karo:
            // Family member cards must re-render after a failed post
            if (sessionData.RegisteredPatient.IsMobileVerified)
            {
                RegisteredPatients = sessionData.RegisteredPatient.PatientList;
                Form.RegisteredPatient.RegistrationNo = sessionData.RegisteredPatient.RegistrationNo;
            }

            if (string.IsNullOrEmpty(Form.NewPatient.StateName))
                Form.NewPatient.StateName = sessionData.NewPatient.StateName;
            if (string.IsNullOrEmpty(Form.NewPatient.CityName))
                Form.NewPatient.CityName = sessionData.NewPatient.CityName;
        }
    }
}
