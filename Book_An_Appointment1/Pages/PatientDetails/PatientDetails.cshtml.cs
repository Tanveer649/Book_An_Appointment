using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Localization;
using Book_An_Appointment1.Security;
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
        private readonly IPatientRegistrationService _registrationService;

        // Development only: Send OTP does not send an SMS and TestOtp is accepted.
        // On any other environment the real OTP APIs are always used.
        private readonly bool _useTestOtp;
        private const string TestOtp = "123456";

        [BindProperty]
        public PatientDetailsViewModel Form { get; set; } = new();

        public AppointmentSessionData? SessionData { get; set; }

        // Family members linked to the verified mobile — rendered as cards (restored on Back / validation failure)
        public List<RegisteredPatientInfo> RegisteredPatients { get; set; } = new();
        public record SelectRegisteredPatientRequest(int? Index);   // position in the verified PatientList

        // Selected family member (-1 = none) — restores the selected card after Back / validation failure
        public int SelectedPatientIndex { get; set; } = -1;
        public record SwitchPatientTypeRequest(string? PatientType);
        public record RememberExtrasRequest(string? Notes, bool ConsentTerms, bool ConsentMarketing);
        public record AddFamilyMemberRequest(int TitleId, string? TitleName, string? FirstName, string? MiddleName, string? LastName,
            string? Dob, string? Gender, string? Email, string? Address, int CountryId, int StateId, int CityId, string? Pincode);

        // How MobSaveRegistrationForPatient reports a wrong / expired OTP ("OTP Or Mobile No is incorrect!")
        private const string DefaultOtpErrorPattern = @"\botp\b";
        private const int NotesMaxLength = 300;   // same as the textarea maxlength

        // How the OTP APIs say "wrong patient type" — override per client in appsettings OtpSettings
        private const string DefaultNotRegisteredPattern = @"not\s+registered|register\s+first";
        private const string DefaultAlreadyRegisteredPattern = @"already\s+(registered|exists?)";

        public PatientDetailsModel(
             IOtpService otpService,
             ILogger<PatientDetailsModel> logger,
             IConfiguration configuration,
             ILocationService locationService,
             ITitleService titleService,
             JsonStringLocalizer localizer,
             IWebHostEnvironment environment,
             IPatientRegistrationService registrationService)
        {
            _localizer = localizer;
            _registrationService = registrationService;
            _useTestOtp = environment.IsDevelopment();
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
                return this.GoToStep(BookingStep.Appointment);

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
                {
                    RegisteredPatients = SessionData.RegisteredPatient.PatientList;
                    SelectedPatientIndex = SessionData.RegisteredPatient.SelectedIndex;
                }

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

            // Registered patients too: the "Add family member" form lists the states of this country
            if (Form.CountryId <= 0)
                Form.CountryId = _configuration.GetValue<int>("DefaultLocationSettings:DefaultCountryId", 1);
        }

        // ── New Patient — Send OTP ───────────────────────────
        public async Task<JsonResult> OnPostSendOtpAsync([FromBody] OtpRequestModel request)
        {
            if (!IsValidMobile(request?.MobileNo))
                return Fail(_localizer.Text("Validation.MobileInvalid"));

            var mobile = request!.MobileNo.Trim();
            try
            {
                // Development: no SMS is sent, test OTP is accepted (see TestOtp)
                if (!_useTestOtp)
                {
                    var result = await _otpService.SendOtpAsync(mobile);

                    if (result?.Status?.ToUpper() != "TRUE")
                    {
                        _logger.LogWarning("SendOtp API returned failure | Mobile={Mobile} Message={Msg}", LogMask.Mobile(mobile), result?.Message);

                        // Number already belongs to a patient → page offers "Continue as registered patient"
                        if (MatchesOtpPattern(result?.Message, "OtpSettings:AlreadyRegisteredPattern", DefaultAlreadyRegisteredPattern))
                            return WrongPatientType("registered", _localizer.Text("Js.AlreadyRegistered"));

                        return Fail(result?.Message ?? _localizer.Text("Errors.OtpSendFailed"));
                    }
                }

                return new JsonResult(new { success = true, message = "OTP sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnPostSendOtpAsync failed | Mobile={Mobile}", LogMask.Mobile(mobile));
                return Fail(_localizer.Text("Errors.SomethingWrong"));
            }
        }

        // ── New Patient — Verify OTP ─────────────────────────
        public async Task<JsonResult> OnPostVerifyOtpAsync([FromBody] VerifyOtpRequestModel request)
        {
            if (!IsValidMobile(request?.MobileNo) || !IsValidOtp(request?.OtpNo))
                return Fail(_localizer.Text("Errors.InvalidOtp"));

            var mobile = request!.MobileNo.Trim();
            try
            {
                if (_useTestOtp)
                {
                    if (request.OtpNo.Trim() != TestOtp)
                    {
                        _logger.LogWarning("VerifyOtp failed (test OTP) | Mobile={Mobile}", LogMask.Mobile(mobile));
                        return Fail(_localizer.Text("Errors.InvalidOtp"));
                    }
                }
                else
                {
                    var result = await _otpService.VerifyOtpAsync(mobile, request.OtpNo.Trim());
                    if (result?.Status?.ToLower() != "success")
                    {
                        _logger.LogWarning("VerifyOtp failed | Mobile={Mobile}", LogMask.Mobile(mobile));
                        return Fail(result?.Message ?? _localizer.Text("Errors.InvalidOtp"));
                    }
                }

                // Verified number is kept in session — the form post is checked against it
                var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session)
                  ?? new AppointmentSessionData();

                sessionData.NewPatient.IsMobileVerified = true;
                sessionData.NewPatient.Mobile = mobile;

                SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

                _logger.LogInformation("VerifyOtp success | Mobile={Mobile}", LogMask.Mobile(mobile));
                return new JsonResult(new { success = true, message = "OTP verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnPostVerifyOtpAsync failed | Mobile={Mobile}", LogMask.Mobile(mobile));
                return Fail(_localizer.Text("Errors.SomethingWrong"));
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
            if (!IsValidMobile(request?.MobileNo))
                return Fail(_localizer.Text("Validation.MobileInvalid"));

            var mobile = request!.MobileNo.Trim();
            _logger.LogInformation("OnPostSendRegisteredOtpAsync | Mobile={M}", LogMask.Mobile(mobile));
            try
            {
                // Development: no SMS is sent, test OTP is accepted (see TestOtp)
                if (!_useTestOtp)
                {
                    var result = await _otpService.SendRegisteredOtpAsync(mobile);

                    if (result?.Status?.ToUpper() != "TRUE")
                    {
                        _logger.LogWarning("SendRegisteredOtp failed | Mobile={M} Message={Msg}", LogMask.Mobile(mobile), result?.Message);

                        // Number not registered yet → page offers "Continue as new patient"
                        if (MatchesOtpPattern(result?.Message, "OtpSettings:NotRegisteredPattern", DefaultNotRegisteredPattern))
                            return WrongPatientType("new", _localizer.Text("Js.NotRegistered"));

                        return Fail(result?.Message ?? _localizer.Text("Errors.OtpSendFailed"));
                    }
                }

                return new JsonResult(new { success = true, message = "OTP sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendRegisteredOtp error | Mobile={M}", LogMask.Mobile(mobile));
                return Fail(_localizer.Text("Errors.SomethingWrong"));
            }
        }

        // ── Registered Patient — Verify OTP + Fetch Details ──
        public async Task<JsonResult> OnPostVerifyRegisteredOtpAsync([FromBody] VerifyOtpRequestModel request)
        {
            if (!IsValidMobile(request?.MobileNo) || !IsValidOtp(request?.OtpNo))
                return Fail(_localizer.Text("Errors.InvalidOtp"));

            request!.MobileNo = request.MobileNo.Trim();
            request.OtpNo = request.OtpNo.Trim();
            _logger.LogInformation("OnPostVerifyRegisteredOtpAsync | Mobile={M}", LogMask.Mobile(request.MobileNo));

            try
            {
                VerifyOtpResponseData? apiData;

                // Development only: test OTP returns the hardcoded family below (other OTPs use the real API)
                if (_useTestOtp && request.OtpNo == TestOtp)
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
                        _logger.LogWarning("VerifyRegisteredOtp failed | Mobile={M}", LogMask.Mobile(request.MobileNo));

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

                // Top-level API response details — the verified number is the one the user entered
                // (the form post is checked against it). The OTP is not part of the booking data; it is kept
                // separately on the server only for "Add family member" (the API checks mobile + OTP).
                registeredPatient.Mobile = request.MobileNo;
                registeredPatient.IsMobileVerified = true;
                // The verify API may return its own OTP value (data.otp) — preferred for MobSaveRegistrationForPatient
                HttpContext.Session.SetString(SessionHelper.FamilyOtpKey,
                    IsValidOtp(apiData.Otp) ? apiData.Otp!.Trim() : request.OtpNo);

                registeredPatient.Otp = null;
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
                    ApplySelectedPatient(registeredPatient, 0);
                else
                    ClearSelectedPatient(registeredPatient);

                SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

                // Only what the page uses (no OTP, no account-level data in the browser)
                return new JsonResult(new
                {
                    success = true,

                    // All family members for the cards
                    patientList = registeredPatient.PatientList,

                    // Currently selected patient (null when user has to choose)
                    selected = selectedPatient != null ? SelectedPatientFields(registeredPatient) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "OnPostVerifyRegisteredOtpAsync failed | Mobile={M}",
                    LogMask.Mobile(request.MobileNo));

                return new JsonResult(new
                {
                    success = false,
                    message = _localizer.Text("Errors.OtpVerifyError")
                });
            }
        }
        // ── Patient type switch (only via "Continue as …") ──
        // The patient cannot toggle the type. It changes only to the type the OTP API just pointed to
        // (see WrongPatientType); the session keeps it because OnPost and Review trust only the session.
        public JsonResult OnPostSwitchPatientType([FromBody] SwitchPatientTypeRequest request)
        {
            var type = request?.PatientType;
            var allowed = HttpContext.Session.GetString(SessionHelper.AllowedTypeSwitchKey);
            if ((type != "new" && type != "registered") || type != allowed)
            {
                _logger.LogWarning("SwitchPatientType refused | Requested={Type} Allowed={Allowed}", type, allowed ?? "-");
                return Fail(_localizer.Text("Errors.SomethingWrong"));
            }

            HttpContext.Session.Remove(SessionHelper.AllowedTypeSwitchKey);   // one switch per API answer

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            if (sessionData == null)
                return new JsonResult(new
                {
                    success = false,
                    message = _localizer.Text("Errors.SessionExpired"),
                    redirectUrl = Url.Content("~" + BookingFlow.BasePath)
                });

            if (sessionData.PatientType != type)
            {
                sessionData.PatientType = type;
                SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);
                _logger.LogInformation("Patient type switched | PatientType={Type}", type);
            }

            return new JsonResult(new { success = true });
        }

        // ── Registered Patient — Select family member card ──
        public JsonResult OnPostSelectRegisteredPatient([FromBody] SelectRegisteredPatientRequest request)
        {
            _logger.LogInformation("OnPostSelectRegisteredPatient | Index={I}", request?.Index);

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            var registeredPatient = sessionData?.RegisteredPatient;

            if (registeredPatient == null || !registeredPatient.IsMobileVerified)
                return new JsonResult(new { success = false, message = _localizer.Text("Validation.VerifyMobile") });

            // Only a member returned for this verified mobile can be selected (by position — works for UHID "0" too)
            var index = request?.Index ?? -1;
            if (index < 0 || index >= registeredPatient.PatientList.Count)
            {
                _logger.LogWarning("SelectRegisteredPatient — not in patient list | Index={I}", request?.Index);
                return new JsonResult(new { success = false, message = _localizer.Text("Errors.PatientNotFound") });
            }

            ApplySelectedPatient(registeredPatient, index);
            SessionHelper.SetAppointmentData(HttpContext.Session, sessionData!);

            return new JsonResult(new { success = true, selected = SelectedPatientFields(registeredPatient) });
        }

        // ── AJAX: Add family member (registered patient) ─────
        // New member under the verified mobile → MobSaveRegistrationForPatient → added to the cards and selected.
        // The API checks mobile + OTP: the OTP the mobile was just verified with is used (no second OTP for the patient).
        public async Task<JsonResult> OnPostAddFamilyMemberAsync([FromBody] AddFamilyMemberRequest request)
        {
            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            var registered = sessionData?.RegisteredPatient;
            if (sessionData == null || registered == null || !registered.IsMobileVerified || !IsValidMobile(registered.Mobile))
                return Fail(_localizer.Text("Validation.VerifyMobile"));

            var invalid = ValidateFamilyMember(request, registered);
            if (invalid != null) return invalid;

            // Gender must fit the title (title master: M / F / B)
            if (!GenderFitsTitle(await TitleGenderAsync(request.TitleId, request.TitleName), request.Gender))
                return new JsonResult(new { success = false, field = "gender", message = _localizer.Text("Validation.GenderTitleMismatch") });

            // OTP of the mobile verification (kept on the server); missing → the mobile must be verified again
            var otp = HttpContext.Session.GetString(SessionHelper.FamilyOtpKey);
            if (!IsValidOtp(otp))
                return Fail(_localizer.Text("Validation.VerifyMobile"));

            var apiRequest = new FamilyMemberRegistrationRequest
            {
                HospitalLocationId = sessionData.HospitalLocationId > 0
                    ? sessionData.HospitalLocationId
                    : _configuration.GetValue("ApiParam:GetFacilitiesParams:HospitalLocationId", 1),
                FacilityId = sessionData.FacilityId.ToString(),
                FirstName = request.FirstName!.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? " " : request.MiddleName.Trim(),
                LastName = request.LastName!.Trim(),
                Dob = DateTime.Parse(request.Dob!, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Gender = GenderCode.Resolve(_configuration, request.Gender, _logger),
                Mobile = registered.Mobile,
                Email = request.Email?.Trim() ?? "",
                TitleId = request.TitleId.ToString(),
                Address = request.Address?.Trim() ?? "",
                CountryId = (request.CountryId > 0 ? request.CountryId : _configuration.GetValue("ApiParam:DefaultLocationSettings:DefaultCountryId", 1)).ToString(),
                StateId = request.StateId.ToString(),
                CityId = request.CityId.ToString(),
                AreaId = _configuration["FamilyMemberSettings:DefaultAreaId"] ?? "0",
                ZipCode = request.Pincode?.Trim() ?? "",
                PaymentFlag = _configuration["FamilyMemberSettings:PaymentFlag"] ?? "0",
                Otp = otp!
            };

            try
            {
                FamilyMemberRegistrationResult result;
                if (_useTestOtp && otp == TestOtp)
                    result = TestFamilyMember(apiRequest, request);   // Development: no record is created in the hospital system
                else
                {
                    result = await _registrationService.SaveFamilyMemberAsync(apiRequest);

                    // The verified OTP is used up by VerifyPatientOTP → the API answers "OTP Or Mobile No is incorrect!".
                    // Ask the OTP API for a fresh OTP for the same verified mobile (its response carries the OTP number)
                    // and retry once — the patient is not asked for anything again.
                    if (!result.Success && MatchesOtpPattern(result.Message, "FamilyMemberSettings:OtpErrorPattern", DefaultOtpErrorPattern))
                    {
                        var freshOtp = await FreshFamilyOtpAsync(registered.Mobile);
                        if (freshOtp != null)
                        {
                            apiRequest.Otp = freshOtp;
                            result = await _registrationService.SaveFamilyMemberAsync(apiRequest);
                            if (result.Success) HttpContext.Session.SetString(SessionHelper.FamilyOtpKey, freshOtp);
                        }
                    }
                }

                if (!result.Success || result.Patient == null)
                {
                    _logger.LogWarning("AddFamilyMember failed | Mobile={M} Message={Msg}", LogMask.Mobile(registered.Mobile), result.Message);

                    // OTP still refused → verify the mobile again for a new one
                    if (MatchesOtpPattern(result.Message, "FamilyMemberSettings:OtpErrorPattern", DefaultOtpErrorPattern))
                        return Fail(_localizer.Text("Errors.FamilyOtpExpired"));
                    return Fail(!string.IsNullOrWhiteSpace(result.Message) ? result.Message : _localizer.Text("Errors.FamilyMemberFailed"));
                }

                // Card data falls back to what was entered when the API leaves a field empty
                var p = result.Patient;
                p.TitleName ??= request.TitleName?.Trim();
                p.TitleId ??= request.TitleId.ToString();
                p.FirstName ??= apiRequest.FirstName;
                p.MiddleName ??= request.MiddleName?.Trim();
                p.LastName ??= apiRequest.LastName;
                p.PatientName ??= string.Join(" ", new[] { request.TitleName, apiRequest.FirstName, request.MiddleName, apiRequest.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
                // The card shows the gender chosen in the form. The API's echo was seen swapped
                // (Ms. + Female sent as code "2" came back "Male"), so it is not trusted for the card;
                // a mismatch is logged because it means AppointmentApi:GenderCodes may not match this HIS.
                if (p.Gender != null && !string.Equals(p.Gender, request.Gender, StringComparison.OrdinalIgnoreCase))
                    _logger.LogWarning("AddFamilyMember — API returned gender {ApiGender} for {Chosen} (sent code {Code}). " +
                                       "Check AppointmentApi:GenderCodes against the HIS gender master.",
                        p.Gender, request.Gender, apiRequest.Gender);
                p.Gender = request.Gender;
                p.Dob ??= apiRequest.Dob;
                p.MobileNo ??= registered.Mobile;
                p.RegistrationNo ??= "0";
                p.FacilityID ??= sessionData.FacilityId.ToString();

                registered.PatientList.Add(p);
                ApplySelectedPatient(registered, registered.PatientList.Count - 1);   // the new member is selected
                SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

                _logger.LogInformation("Family member added | Mobile={M} GuestPatientId={G}", LogMask.Mobile(registered.Mobile), p.GuestPatientId);

                return new JsonResult(new
                {
                    success = true,
                    patientList = registered.PatientList,
                    selected = SelectedPatientFields(registered)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddFamilyMember error | Mobile={M}", LogMask.Mobile(registered.Mobile));
                return Fail(_localizer.Text("Errors.FamilyMemberFailed"));
            }
        }

        // New OTP for the verified mobile, read from the OTP API's own response (Data[].OtpNo) — server side only.
        // null when the API does not return the number (then the patient re-verifies the mobile).
        private async Task<string?> FreshFamilyOtpAsync(string mobile)
        {
            try
            {
                var response = await _otpService.SendRegisteredOtpAsync(mobile);
                var otp = response?.Data?.FirstOrDefault(d => string.IsNullOrWhiteSpace(d.MobileNo) || d.MobileNo.Trim().EndsWith(mobile[^4..]))?.OtpNo?.Trim();
                if (response?.Status?.ToUpper() == "TRUE" && IsValidOtp(otp))
                {
                    _logger.LogInformation("AddFamilyMember — fresh OTP obtained | Mobile={M}", LogMask.Mobile(mobile));
                    return otp;
                }
                _logger.LogWarning("AddFamilyMember — OTP API returned no OTP number | Mobile={M} Status={S} Message={Msg}",
                    LogMask.Mobile(mobile), response?.Status, response?.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddFamilyMember — fresh OTP request failed | Mobile={M}", LogMask.Mobile(mobile));
            }
            return null;
        }

        // Server-side checks (the page checks the same before sending). Returns the error response or null.
        // Title master (GetTitles) "Gender": M = male title (Mr., Master), F = female title (Mrs., Ms., Baby),
        // B = both (Dr., Baby Of) → any gender. Returns null when the title is unknown or the API is down.
        private async Task<string?> TitleGenderAsync(int titleId, string? titleName)
        {
            try
            {
                var titles = (await _titleService.GetTitlesAsync())?.TitleList;
                var title = titles?.FirstOrDefault(t => titleId > 0
                    ? t.TitleId == titleId
                    : string.Equals(t.Name?.Trim(), titleName?.Trim(), StringComparison.OrdinalIgnoreCase));
                return title?.Gender?.Trim().ToUpperInvariant();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Title gender lookup failed | TitleId={T}", titleId);
                return null;   // not blocked when the title list cannot be read
            }
        }

        // M → Male only, F → Female only, B / unknown → any gender
        private static bool GenderFitsTitle(string? titleGender, string? gender) => titleGender switch
        {
            "M" => gender == "Male",
            "F" => gender == "Female",
            _ => true
        };

        private JsonResult? ValidateFamilyMember(AddFamilyMemberRequest? r, AppointmentSessionData.RegisteredPatientData registered)
        {
            JsonResult Field(string field, string key) => new(new { success = false, field, message = _localizer.Text(key) });
            var name = new Regex(@"^[a-zA-Z\s'\-]{1,50}$");

            if (r == null) return Fail(_localizer.Text("Errors.SomethingWrong"));
            if (r.TitleId <= 0) return Field("title", "Validation.SelectTitle");
            if (string.IsNullOrWhiteSpace(r.FirstName)) return Field("firstName", "Validation.FirstNameRequired");
            if (!name.IsMatch(r.FirstName.Trim())) return Field("firstName", "Validation.FirstNameLetters");
            if (!string.IsNullOrWhiteSpace(r.MiddleName) && !name.IsMatch(r.MiddleName.Trim())) return Field("middleName", "Validation.MiddleNameLetters");
            if (string.IsNullOrWhiteSpace(r.LastName)) return Field("lastName", "Validation.LastNameRequired");
            if (!name.IsMatch(r.LastName.Trim())) return Field("lastName", "Validation.LastNameLetters");

            if (!DateTime.TryParseExact(r.Dob, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                return Field("dob", "Validation.DobRequired");
            if (dob >= DateTime.Today || dob < DateTime.Today.AddYears(-120)) return Field("dob", "Validation.DobPast");

            if (r.Gender is not ("Male" or "Female" or "Other")) return Field("gender", "Validation.SelectGender");
            if (!string.IsNullOrWhiteSpace(r.Email) && (r.Email.Length > 100 || !Regex.IsMatch(r.Email.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$")))
                return Field("email", "Validation.EmailInvalid");
            if (r.Address?.Length > 200) return Field("address", "Validation.AddressTooLong");
            if (r.StateId <= 0) return Field("state", "Validation.SelectState");
            if (r.CityId <= 0) return Field("city", "Validation.SelectCity");
            if (!string.IsNullOrWhiteSpace(r.Pincode) && !Regex.IsMatch(r.Pincode.Trim(), @"^\d{6}$")) return Field("pincode", "Validation.PincodeInvalid");

            // Same person already on this mobile → pick the existing card instead
            var exists = registered.PatientList.Any(p =>
                string.Equals(p.FirstName?.Trim(), r.FirstName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.LastName?.Trim(), r.LastName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                DateTime.TryParse(p.Dob, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && d.Date == dob.Date);
            return exists ? Fail(_localizer.Text("Errors.FamilyMemberExists")) : null;
        }

        // Development only (test OTP): imitates a successful MobSaveRegistrationForPatient
        private static FamilyMemberRegistrationResult TestFamilyMember(FamilyMemberRegistrationRequest api, AddFamilyMemberRequest r)
        {
            var dob = DateTime.Parse(api.Dob, CultureInfo.InvariantCulture);
            var age = DateTime.Today.Year - dob.Year - (DateTime.Today.DayOfYear < dob.DayOfYear ? 1 : 0);
            return new FamilyMemberRegistrationResult
            {
                Success = true,
                Message = "Record Saved!",
                Patient = new RegisteredPatientInfo
                {
                    HospitalLocationId = api.HospitalLocationId.ToString(CultureInfo.InvariantCulture),
                    FacilityID = api.FacilityId,
                    RegistrationNo = "0",
                    GuestPatientId = Random.Shared.Next(50000, 99999).ToString(),
                    PatientName = $"{r.TitleName} {api.FirstName} {api.LastName}".Trim(),
                    TitleId = api.TitleId,
                    TitleName = r.TitleName,
                    FirstName = api.FirstName,
                    MiddleName = api.MiddleName.Trim(),
                    LastName = api.LastName,
                    Gender = r.Gender,
                    Dob = api.Dob,
                    Age = $"{age} Yrs/{r.Gender}",
                    EmailId = api.Email,
                    MobileNo = api.Mobile,
                    IsUnRegPat = true
                }
            };
        }

        // ── AJAX: Remember notes + consent ticks ─────────────
        // So a page reload keeps them. Only a draft: the Review & Confirm submit validates them again.
        public JsonResult OnPostRememberExtras([FromBody] RememberExtrasRequest request)
        {
            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            if (sessionData == null || request == null)
                return new JsonResult(new { success = false });

            var notes = request.Notes?.Trim();
            if (notes?.Length > NotesMaxLength) notes = notes[..NotesMaxLength];

            sessionData.AppointmentNotes = notes;
            sessionData.ConsentAccepted = request.ConsentTerms;
            sessionData.ConsentMarketing = request.ConsentMarketing;
            SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

            return new JsonResult(new { success = true });
        }

        private static void ApplySelectedPatient(AppointmentSessionData.RegisteredPatientData registeredPatient, int index)
        {
            var selectedPatient = registeredPatient.PatientList[index];
            registeredPatient.SelectedIndex = index;

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
            registeredPatient.SelectedIndex = -1;
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
            selectedIndex = p.SelectedIndex,
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
        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("PatientDetails OnPost | PatientType={Type}", Form.PatientType);

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            if (sessionData == null)
                return this.GoToStep(BookingStep.Appointment);

            ModelState.Clear();

            // Patient type was chosen on page 1 and is locked — never taken from the posted form
            Form.PatientType = sessionData.PatientType == "registered" ? "registered" : "new";

            // ── Validation — PatientType ke basis pe ──────
            if (Form.PatientType == "new" || string.IsNullOrEmpty(Form.PatientType))
            {
                ValidateNewPatient(sessionData);

                // Gender must fit the title (title master: M / F / B)
                var np = Form.NewPatient;
                if (!string.IsNullOrWhiteSpace(np.Gender) &&
                    !GenderFitsTitle(await TitleGenderAsync(np.TitleId, np.TitleName), np.Gender))
                    ModelState.AddModelError("Form.NewPatient.Gender", _localizer.Text("Validation.GenderTitleMismatch"));
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
                HttpContext.Session.Remove(SessionHelper.FamilyOtpKey);
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

            // Patient details valid → step 3 (/reviewconfirm/{new id})
            return this.GoToStep(BookingStep.Review);
        }

        // "← Back" button (anti-forgery checked) — the only way back to step 1; typing URLs cannot do it
        public IActionResult OnPostBack() => this.GoToStep(BookingStep.Appointment);

        // ── Request Helpers (AJAX handlers) ────────────────
        private static bool IsValidMobile(string? mobile) =>
            !string.IsNullOrWhiteSpace(mobile) && Regex.IsMatch(mobile.Trim(), @"^\d{10}$");

        private bool IsValidOtp(string? otp) =>
            !string.IsNullOrWhiteSpace(otp) &&
            Regex.IsMatch(otp.Trim(), $@"^\d{{{_configuration.GetValue<int>("OtpSettings:OtpLength", 6)}}}$");

        private static JsonResult Fail(string message) => new(new { success = false, message });

        // Number belongs to the other patient type — the page shows "Continue as …" and the session
        // remembers that exactly this switch is allowed (OnPostSwitchPatientType)
        private JsonResult WrongPatientType(string targetType, string message)
        {
            HttpContext.Session.SetString(SessionHelper.AllowedTypeSwitchKey, targetType);
            return new JsonResult(new { success = false, wrongType = true, message });
        }

        private bool MatchesOtpPattern(string? apiMessage, string configKey, string defaultPattern)
        {
            if (string.IsNullOrWhiteSpace(apiMessage)) return false;
            var pattern = _configuration[configKey];
            try
            {
                return Regex.IsMatch(apiMessage, string.IsNullOrWhiteSpace(pattern) ? defaultPattern : pattern,
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            }
            catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)   // bad pattern in appsettings
            {
                _logger.LogError(ex, "Invalid regex in {Key}", configKey);
                return false;
            }
        }

        // ── Validation Helpers ─────────────────────────────
        private void ValidateNewPatient(AppointmentSessionData sessionData)
        {
            var p = Form.NewPatient;
            var saved = sessionData.NewPatient;   // OTP result is trusted from session, not the hidden input

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

            if (!p.IsMobileVerified || !saved.IsMobileVerified || saved.Mobile != p.Mobile?.Trim())
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
            else if (saved.SelectedIndex < 0 || saved.SelectedIndex >= saved.PatientList.Count)
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
                SelectedPatientIndex = sessionData.RegisteredPatient.SelectedIndex;
                Form.RegisteredPatient.RegistrationNo = sessionData.RegisteredPatient.RegistrationNo;
            }

            if (string.IsNullOrEmpty(Form.NewPatient.StateName))
                Form.NewPatient.StateName = sessionData.NewPatient.StateName;
            if (string.IsNullOrEmpty(Form.NewPatient.CityName))
                Form.NewPatient.CityName = sessionData.NewPatient.CityName;
        }
    }
}
