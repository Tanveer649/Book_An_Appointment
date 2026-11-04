using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Localization;
using Book_An_Appointment1.Security;
using Book_An_Appointment1.Models.Slot;
using Book_An_Appointment1.Services.Interfaces;
using Book_An_Appointment1.ViewModels.Appointment;
using Book_An_Appointment1.ViewModule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Book_An_Appointment1.Pages.Appointment
{
    public class AppointmentModel : PageModel
    {
        private readonly IFacilityService _facilityService;
        private readonly ISpecialityService _specialityService;
        private readonly IDoctorService _doctorService;
        private readonly IConsultationService _consultationService;
        private readonly ISlotService _slotService;
        private readonly ILogger<AppointmentModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly JsonStringLocalizer _localizer;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string ImageSessionKey = "AllowedDoctorImages";

        public AppointmentViewModel Wizard { get; set; } = new();
        public record GetDoctorsRequest(int FacilityId, int SpecialityId);
        public record GetDoctorDetailsRequest(int FacilityId, int SpecialityId, int DoctorId);
        public record RememberSlotRequest(string? Date, string? Start);

        [BindProperty] public int FacilityId { get; set; }
        [BindProperty] public int? SpecialityId { get; set; }
        [BindProperty] public int? DoctorId { get; set; }
        public string? SavedSlotDate { get; set; }
        public string? SavedSlotStart { get; set; }
        [BindProperty] public string? ErrorMessage { get; set; }


        public AppointmentModel(
            IFacilityService facilityService,
            ISpecialityService specialityService,
            IDoctorService doctorService,
            IConsultationService consultationService,
            ISlotService slotService,
            ILogger<AppointmentModel> logger,
            IConfiguration configuration,
            JsonStringLocalizer localizer,
            IHttpClientFactory httpClientFactory)
        {
            _localizer = localizer;
            _httpClientFactory = httpClientFactory;
            _facilityService = facilityService;
            _specialityService = specialityService;
            _doctorService = doctorService;
            _consultationService = consultationService;
            _slotService = slotService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("OnGetAsync started");

            Wizard.MaxAppointmentAllowedDays = _configuration.GetValue<int>("MaxAppointmentAllowedDays:Days", 90);

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);

            await LoadFacilities();

            if (sessionData != null && sessionData.FacilityId > 0)
            {
                FacilityId = sessionData.FacilityId;
                SpecialityId = sessionData.SpecialityId > 0 ? sessionData.SpecialityId : null;
                DoctorId = sessionData.DoctorId > 0 ? sessionData.DoctorId : null;
                // A remembered date that is already in the past is dropped — the calendar starts from today again
                if (!string.IsNullOrEmpty(sessionData.SlotDate) && string.CompareOrdinal(sessionData.SlotDate,
                        DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) < 0)
                {
                    sessionData.SlotDate = "";
                    sessionData.SlotTime = "";
                }
                SavedSlotDate = sessionData.SlotDate;
                SavedSlotStart = sessionData.SlotTime;

                await LoadSpecialities();

                if (SpecialityId.HasValue && SpecialityId > 0)
                {
                    await LoadDoctors();

                    if (DoctorId.HasValue && DoctorId > 0)
                    {
                        await LoadDoctorDetails();

                        var dateToLoad = !string.IsNullOrEmpty(sessionData.SlotDate) ? sessionData.SlotDate : DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                        var slotResponse = await _slotService.GetSlotsAsync(FacilityId, DoctorId ?? 0, sessionData.HospitalLocationId, dateToLoad, dateToLoad);

                        Wizard.Slots = slotResponse?.Data?.SlotsDetails ?? new List<SlotResponse>();
                    }
                }

                _logger.LogInformation(
                    "Session restored | FacilityId={F} SpecialityId={S} DoctorId={D}",
                    FacilityId, SpecialityId, DoctorId);
            }
        }

        // ── AJAX: Get Specialities ───────────────────────────
        public async Task<JsonResult> OnPostGetSpecialitiesAsync([FromBody] int facilityId)
        {
            _logger.LogInformation("OnPostGetSpecialitiesAsync | FacilityId={F}", facilityId);
            try
            {
                FacilityId = facilityId;
                await LoadSpecialities();
                return new JsonResult(new
                {
                    success = true,
                    data = Wizard.Specialities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSpecialities failed | FacilityId={F}", facilityId);
                return new JsonResult(new
                {
                    success = false,
                    data = new List<object>()
                });
            }
        }

        // ── AJAX: Get Doctors ────────────────────────────────
        public async Task<JsonResult> OnPostGetDoctorsAsync([FromBody] GetDoctorsRequest request)
        {
            _logger.LogInformation("OnPostGetDoctorsAsync | FacilityId={F} SpecialityId={S}",
                request.FacilityId, request.SpecialityId);
            try
            {
                FacilityId = request.FacilityId;
                SpecialityId = request.SpecialityId;
                await LoadDoctors();
                return new JsonResult(new
                {
                    success = true,
                    data = Wizard.Doctors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDoctors failed | FacilityId={F} SpecialityId={S}",
                    request.FacilityId, request.SpecialityId);
                return new JsonResult(new
                {
                    success = false,
                    data = new List<object>()
                });
            }
        }

        // ── AJAX: Get Doctor Details + Save Session ──────────
        public async Task<JsonResult> OnPostGetDoctorDetailsAsync([FromBody] GetDoctorDetailsRequest request)
        {
            _logger.LogInformation("OnPostGetDoctorDetailsAsync | FacilityId={F} SpecialityId={S} DoctorId={D}",
                request.FacilityId, request.SpecialityId, request.DoctorId);
            try
            {
                FacilityId = request.FacilityId;
                SpecialityId = request.SpecialityId;
                DoctorId = request.DoctorId;

                await LoadDoctors();
                await LoadDoctorDetails();

                var doc = Wizard.SelectedDoctor;
                var hospital = doc?.ConsultingHospitals?.FirstOrDefault();

                var feeRaw = Wizard.ConsultationFeeList?.FirstOrDefault()?.NetAmount
                               ?? Wizard.ConsultationFeeList?.FirstOrDefault()?.Price
                               ?? "0";
                var RegfeeRaw = Wizard.RegistrationFeeList?.FirstOrDefault()?.NetAmount
                               ?? Wizard.RegistrationFeeList?.FirstOrDefault()?.Price
                               ?? "0";

                // Trailing zeros remove karo
                var fee = FormatFee(feeRaw);

                var Regfee = FormatFee(RegfeeRaw);

                var existing = SessionHelper.GetAppointmentData(HttpContext.Session)
                               ?? new AppointmentSessionData();

                // Another doctor → the remembered date/slot belonged to the previous one
                if (existing.DoctorId != (DoctorId ?? 0) || existing.FacilityId != FacilityId)
                {
                    existing.SlotDate = "";
                    existing.SlotTime = "";
                    existing.SlotDisplayText = "";
                }

                existing.FacilityId = FacilityId;
                existing.DoctorId = DoctorId ?? 0;
                existing.SpecialityId = SpecialityId ?? 0;
                existing.HospitalLocationId = doc?.HospitalLocationId ?? 0;
                existing.DoctorName = doc?.FullName ?? "";
                existing.DoctorInitials = GetInitials(doc?.FullName ?? "");
                existing.Specialisation = doc?.SpecialisationName ?? "";
                existing.Hospital = hospital?.FacilityName ?? "";

                existing.ConsultationFee = decimal.TryParse(feeRaw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0;

                existing.RegistrationFee = decimal.TryParse(RegfeeRaw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var Rf) ? Rf : 0;

                SessionHelper.SetAppointmentData(HttpContext.Session, existing);

                _logger.LogInformation("DoctorDetails session saved | Name={N} Hospital={H} Fee={Fee}",
                    existing.DoctorName, existing.Hospital, existing.ConsultationFee);

                return new JsonResult(new
                {
                    success = true,
                    data = new
                    {
                        name = doc?.FullName ?? "",
                        specialisation = doc?.SpecialisationName ?? "",
                        experience = doc?.Experience ?? "",
                        description = doc?.Description ?? "",
                        imageUrl = doc?.ImageUrl ?? "",
                        rating = doc?.Rating ?? "",
                        review = doc?.Review ?? "",
                        treatment = doc?.Treatment ?? "",
                        timing = doc?.Timing ?? "",
                        isTeleconsult = doc?.IsTeleconsult ?? false,
                        fee = fee,
                        regFee = Regfee,
                        hospitalName = hospital?.FacilityName ?? "",
                        hospitalCity = hospital?.CityName ?? "",
                        hospitalLocationId = doc?.HospitalLocationId ?? 0,
                        icon = doc?.Icon ?? "",
                        education = doc?.Education ?? "",
                        slots = Wizard.Slots.Select(s => new
                        {
                            startTime = s.StartTime,
                            endTime = s.EndTime
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDoctorDetails failed");
                return new JsonResult(new { success = false });
            }
        }

        // ── GET: Slots by Date ───────────────────────────────
        public async Task<JsonResult> OnGetSlotsByDateAsync(int facilityId, int? doctorId, int hospitalLocationId, string date)
        {
            // Only a real yyyy-MM-dd date is passed on to the hospital API
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return new JsonResult(new List<SlotResponse>());

            try
            {
                var slots = await GetDoctorSlotsAsync(facilityId, doctorId ?? 0, hospitalLocationId, date);

                return new JsonResult(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnGetSlotsByDateAsync failed | FacilityId={F} DoctorId={D} Date={Date}",
                    facilityId, doctorId, date);
                return new JsonResult(new List<SlotResponse>());
            }
        }

        // ── AJAX: Remember the picked date / slot ────────────
        // Lets a page reload restore the user's choice. It is only a draft: Continue still re-checks
        // the slot against the doctor's live slot list before the booking moves on.
        public JsonResult OnPostRememberSlot([FromBody] RememberSlotRequest request)
        {
            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            if (sessionData == null || sessionData.DoctorId <= 0)
                return new JsonResult(new { success = false });

            if (!DateTime.TryParseExact(request?.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                return new JsonResult(new { success = false });

            var start = request?.Start?.Trim();
            if (!string.IsNullOrEmpty(start) &&
                (start.Length > 40 ||
                 !DateTime.TryParse(start, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startAt) ||
                 startAt.Date != day.Date))
                return new JsonResult(new { success = false });

            sessionData.SlotDate = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sessionData.SlotTime = start ?? "";
            SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);
            return new JsonResult(new { success = true });
        }

        // ── POST: Continue Button ────────────────────────────
        // MODIFIED: Validation fail hone pe ab poora Wizard reload karta hai
        // taaki dropdowns/doctor card UI mein blank na dikhein

        public async Task<IActionResult> OnPostContinueAsync(string selectedSlotStart, string selectedSlotEnd,
            int facilityId, int? specialityId, int? doctorId, string? patientType)
        {
            _logger.LogInformation("OnPostContinueAsync | Slot={Slot} FacilityId={F} SpecialityId={S} DoctorId={D} PatientType={P}",
                selectedSlotStart, facilityId, specialityId, doctorId, patientType);

            ModelState.Clear();

            FacilityId = facilityId;
            SpecialityId = specialityId;
            DoctorId = doctorId;

            // ── Server Side Validation ─────────────────────
            if (facilityId <= 0)
                ModelState.AddModelError("Facility", _localizer.Text("Validation.SelectFacility"));

            if (!specialityId.HasValue || specialityId <= 0)
                ModelState.AddModelError("Speciality", _localizer.Text("Validation.SelectSpeciality"));

            if (!doctorId.HasValue || doctorId <= 0)
                ModelState.AddModelError("Doctor", _localizer.Text("Validation.SelectDoctor"));

            if (string.IsNullOrEmpty(selectedSlotStart))
                ModelState.AddModelError("Slot", _localizer.Text("Validation.SelectSlot"));

            // ── Server Side Validation — doctor + slot must be what this session loaded ──
            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);
            if (ModelState.IsValid)
            {
                if (sessionData == null || sessionData.FacilityId != facilityId || sessionData.DoctorId != doctorId)
                {
                    ModelState.AddModelError("Doctor", _localizer.Text("Errors.SelectDoctorAgain"));
                }
                else
                {
                    // Slot time is never trusted from the browser: it must be a free slot of this doctor
                    var slot = await FindAvailableSlotAsync(sessionData, selectedSlotStart);
                    if (slot == null)
                        ModelState.AddModelError("Slot", _localizer.Text("Errors.SlotUnavailable"));
                    else
                        selectedSlotEnd = slot.EndTime;
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("OnPostContinueAsync validation failed");

                // Wizard reload karo taaki UI properly render ho
                await LoadFacilities();

                if (facilityId > 0)
                {
                    await LoadSpecialities();

                    if (specialityId.HasValue && specialityId > 0)
                    {
                        await LoadDoctors();

                        if (doctorId.HasValue && doctorId > 0)
                            await LoadDoctorDetails();
                    }
                }

                return Page();
            }

            var type = string.Equals(patientType, "registered", StringComparison.OrdinalIgnoreCase) ? "registered" : "new";


            // ── Session Save ──────────────────────────────
            sessionData!.PatientType = type;
            sessionData.SlotTime = selectedSlotStart;
            HttpContext.Session.SetString("Booking.SlotEnd", selectedSlotEnd ?? ""); 
            // Machine format — invariant so the chosen portal language never changes it
            sessionData.SlotDate = DateTime.TryParse(selectedSlotStart, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedSlot)
                       ? parsedSlot.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                       : DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sessionData.SlotDisplayText = FormatSlotDisplay(selectedSlotStart);

            SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

            _logger.LogInformation("Session slot saved | SlotDisplayText={S}", sessionData.SlotDisplayText);

            // Slot verified → step 2 (/patientdetails/{new id}); the step-1 URL stops working
            return this.GoToStep(BookingStep.PatientDetails);
        }

        // Slot from the doctor's live slot list for that day; null when it is not offered or already passed
        private async Task<SlotResponse?> FindAvailableSlotAsync(AppointmentSessionData sessionData, string slotStart)
        {
            if (!DateTime.TryParse(slotStart, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) || start <= DateTime.Now)
                return null;

            var date = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var slots = await GetDoctorSlotsAsync(sessionData.FacilityId, sessionData.DoctorId, sessionData.HospitalLocationId, date);
            var slot = slots.FirstOrDefault(s => string.Equals(s.StartTime?.Trim(), slotStart.Trim(), StringComparison.Ordinal));

            if (slot == null)
                _logger.LogWarning("Continue — slot not available | DoctorId={D} Slot={S}", sessionData.DoctorId, slotStart);

            return slot;
        }

        // ── Private Loaders ──────────────────────────────────
        private async Task LoadFacilities()
        {
            try
            {
                Wizard.Facilities = (await _facilityService.GetFacilitiesAsync())?.Data ?? new();
                _logger.LogInformation("LoadFacilities | Count={C}", Wizard.Facilities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadFacilities failed");
                Wizard.Facilities = new();
            }
        }

        private async Task LoadSpecialities()
        {
            try
            {
                Wizard.Specialities = (await _specialityService.GetSpecialitiesAsync(FacilityId))?.Data ?? new();
                _logger.LogInformation("LoadSpecialities | Count={C}", Wizard.Specialities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadSpecialities failed | FacilityId={F}", FacilityId);
                Wizard.Specialities = new();
            }
        }

        private async Task LoadDoctors()
        {
            try
            {
                Wizard.Doctors = (await _doctorService.GetDoctorsAsync(FacilityId, SpecialityId ?? 0))?.Data ?? new();
                _logger.LogInformation("LoadDoctors | Count={C}", Wizard.Doctors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadDoctors failed | FacilityId={F} SpecialityId={S}", FacilityId, SpecialityId);
                Wizard.Doctors = new();
            }
        }

        private async Task LoadDoctorDetails()
        {
            try
            {
                Wizard.SelectedDoctor = Wizard.Doctors.FirstOrDefault(x => x.Id == (DoctorId ?? 0).ToString());

                if (Wizard.SelectedDoctor == null)
                {
                    _logger.LogWarning("Doctor not found | DoctorId={D}", DoctorId);
                    return;
                }

                RegisterDoctorImages(Wizard.SelectedDoctor.Icon, Wizard.SelectedDoctor.ImageUrl);

                var today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                var consultationTask = _consultationService.GetConsultationFeeAsync(FacilityId, (DoctorId ?? 0));

                var slotsTask = GetDoctorSlotsAsync(FacilityId, DoctorId ?? 0, Wizard.SelectedDoctor.HospitalLocationId, today); 

                await Task.WhenAll(consultationTask, slotsTask);  

                Wizard.Slots = await slotsTask;
                var consultantResponse = await consultationTask;

                // Consultation Fee
                Wizard.ConsultationFeeList = consultantResponse?.Where(x => x.ServiceType == "CL").ToList() ?? new();

                // Registration charge in case of Unregister patient

                Wizard.RegistrationFeeList = consultantResponse?
                                            .Where(x =>
                                                string.Equals(x.ServiceType, "RF", StringComparison.OrdinalIgnoreCase) &&
                                                string.Equals(x.ServiceName?.Trim(), "Registration Charges",
                                                    StringComparison.OrdinalIgnoreCase))
                                            .ToList() ?? new();

                _logger.LogInformation("LoadDoctorDetails | Slots={S} Fees={F}",
                    Wizard.Slots.Count, Wizard.ConsultationFeeList.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadDoctorDetails failed | DoctorId={D} FacilityId={F}", DoctorId, FacilityId);
                Wizard.Slots = new();
                Wizard.ConsultationFeeList = new();
                Wizard.RegistrationFeeList = new();
            }
        }

        private async Task<List<SlotResponse>> GetDoctorSlotsAsync(int facilityId, int doctorId, int hospitalLocationId, string date)
        {
            try
            {
                var slotResponse = await _slotService.GetSlotsAsync(facilityId, doctorId, hospitalLocationId, date, date);

                return slotResponse?.Data?.SlotsDetails ?? new List<SlotResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "GetDoctorSlotsAsync failed | FacilityId={FacilityId}, DoctorId={DoctorId}, HospitalLocationId={HospitalLocationId}, Date={Date}",
                    facilityId,
                    doctorId,
                    hospitalLocationId,
                    date);

                return new List<SlotResponse>();
            }
        }

        // ── Helpers ──────────────────────────────────────────
        private static string GetInitials(string name) =>
            string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2).Select(x => char.ToUpper(x[0])));

        // Display text in the current portal language (also used by PatientDetails, so a language
        // switch after choosing the slot still shows the slot in the new language)
        public static string FormatSlotDisplay(string slotTime)
        {
            if (DateTime.TryParse(slotTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.ToString("ddd, d MMM · hh:mm tt");

            if (TimeSpan.TryParse(slotTime, CultureInfo.InvariantCulture, out var time))
            {
                var date = DateTime.Now.ToString("ddd, d MMM");
                var timeStr = DateTime.Today.Add(time).ToString("hh:mm tt");
                return $"{date} · {timeStr}";
            }
            return slotTime;
        }

        public static string FormatFee(string? raw) =>
                decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                         System.Globalization.CultureInfo.InvariantCulture, out var d)
            ? Math.Round(d, 0, MidpointRounding.AwayFromZero).ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            : (raw ?? "0");

        private void RegisterDoctorImages(params string?[] urls)
        {
            var list = (HttpContext.Session.GetString(ImageSessionKey) ?? "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var u in urls)
            {
                var url = u?.Trim();
                if (!string.IsNullOrEmpty(url) && !list.Contains(url)) list.Add(url);
            }
            if (list.Count > 50) list = list.Skip(list.Count - 50).ToList();
            HttpContext.Session.SetString(ImageSessionKey, string.Join('\n', list));
        }

        public async Task<IActionResult> OnGetDoctorImageAsync(string url)
        {
            var allowed = (HttpContext.Session.GetString(ImageSessionKey) ?? "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (string.IsNullOrWhiteSpace(url) || !allowed.Contains(url, StringComparer.Ordinal) ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return NotFound();

            try
            {
                using var resp = await _httpClientFactory.CreateClient("DoctorImageClient").GetAsync(uri);
                var type = resp.Content.Headers.ContentType?.MediaType ?? "";
                if (!resp.IsSuccessStatusCode || !type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return NotFound();
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                if (bytes.Length > 2_000_000) return NotFound();
                Response.Headers["Cache-Control"] = "private,max-age=86400";
                return File(bytes, type);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DoctorImage fetch failed | Url={U}", url);
                return NotFound();
            }
        }
    }
}