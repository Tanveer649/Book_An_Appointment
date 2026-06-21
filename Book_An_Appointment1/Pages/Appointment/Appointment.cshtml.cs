using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Models.Slot;
using Book_An_Appointment1.Services.Interfaces;
using Book_An_Appointment1.ViewModels.Appointment;
using Book_An_Appointment1.ViewModule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public AppointmentViewModel Wizard { get; set; } = new();
        public record GetDoctorsRequest(int FacilityId, int SpecialityId);
        public record GetDoctorDetailsRequest(int FacilityId, int SpecialityId, int DoctorId);


        [BindProperty] public int FacilityId { get; set; }
        [BindProperty] public int? SpecialityId { get; set; }
        [BindProperty] public int? DoctorId { get; set; }
        [BindProperty] public string? ErrorMessage { get; set; }

        public AppointmentModel(
            IFacilityService facilityService,
            ISpecialityService specialityService,
            IDoctorService doctorService,
            IConsultationService consultationService,
            ISlotService slotService,
            ILogger<AppointmentModel> logger)
        {
            _facilityService = facilityService;
            _specialityService = specialityService;
            _doctorService = doctorService;
            _consultationService = consultationService;
            _slotService = slotService;
            _logger = logger;
        }

        // ── GET ─────────────────────────────────────────────
        public async Task OnGetAsync()
        {
            _logger.LogInformation("OnGetAsync started");
            await LoadFacilities();
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
                _logger.LogError(ex,"GetSpecialities failed | FacilityId={F}", facilityId);
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
            _logger.LogInformation( "OnPostGetDoctorsAsync | FacilityId={F} SpecialityId={S}",
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
                _logger.LogError(ex,
                    "GetDoctors failed | FacilityId={F} SpecialityId={S}",
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
                var fee = Wizard.ConsultationFeeList?.FirstOrDefault()?.Price
                               ?? Wizard.ConsultationFeeList?.FirstOrDefault()?.NetAmount
                               ?? "0";

                // Session mein doctor data save karo
                var existing = SessionHelper.GetAppointmentData(HttpContext.Session)
                               ?? new AppointmentSessionData();

                existing.FacilityId = FacilityId;
                existing.DoctorId = DoctorId ?? 0;
                existing.SpecialityId = SpecialityId ?? 0;
                existing.HospitalLocationId = doc?.HospitalLocationId ?? 0;
                existing.DoctorName = doc?.FullName ?? "";
                existing.DoctorInitials = GetInitials(doc?.FullName ?? "");
                existing.Specialisation = doc?.SpecialisationName ?? "";
                existing.Hospital = hospital?.FacilityName ?? "";
                existing.ConsultationFee = decimal.TryParse(fee, out var f) ? f : 0;

                SessionHelper.SetAppointmentData(HttpContext.Session, existing);

                _logger.LogInformation(
                    "DoctorDetails session saved | Name={N} Hospital={H} Fee={Fee}",
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
                        hospitalName = hospital?.FacilityName ?? "",
                        hospitalCity = hospital?.CityName ?? "",
                        hospitalLocationId = doc?.HospitalLocationId ?? 0,
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
            try
            {
                var slotResponse = await _slotService.GetSlotsAsync(
                    facilityId, (doctorId ?? 0), hospitalLocationId, date, date);

                var slots = slotResponse?.Data?.SlotsDetails ?? new List<SlotResponse>();
                return new JsonResult(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OnGetSlotsByDateAsync failed | FacilityId={F} DoctorId={D} Date={Date}",
                    facilityId, doctorId, date);
                return new JsonResult(new List<SlotResponse>());
            }
        }

        // ── POST: Continue Button ────────────────────────────
        public IActionResult OnPostContinueAsync(string selectedSlotStart, string selectedSlotEnd)
        {
            _logger.LogInformation(
                "OnPostContinueAsync | Slot={Slot}", selectedSlotStart);

            ModelState.Clear();

            if (string.IsNullOrEmpty(selectedSlotStart))
            {
                ModelState.AddModelError("Slot", "Please select a time slot.");
                return Page();
            }

            // Session se existing doctor data lo — sirf slot add karo
            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session)
                              ?? new AppointmentSessionData();

            sessionData.SlotTime = selectedSlotStart;
            sessionData.SlotDate = DateTime.Now.ToString("yyyy-MM-dd");
            sessionData.SlotDisplayText = FormatSlotDisplay(selectedSlotStart);

            SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

            _logger.LogInformation(
                "Session slot saved | SlotDisplayText={S}", sessionData.SlotDisplayText);

            return RedirectToPage("/PatientDetails/PatientDetails");
        }

        // ── Private Loaders ──────────────────────────────────
        private async Task LoadFacilities()
        {
            try
            {
                Wizard.Facilities =
                    (await _facilityService.GetFacilitiesAsync())?.Data ?? new();
                _logger.LogInformation(
                    "LoadFacilities | Count={C}", Wizard.Facilities.Count);
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
                Wizard.Specialities =
                    (await _specialityService.GetSpecialitiesAsync(FacilityId))?.Data ?? new();
                _logger.LogInformation(
                    "LoadSpecialities | Count={C}", Wizard.Specialities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LoadSpecialities failed | FacilityId={F}", FacilityId);
                Wizard.Specialities = new();
            }
        }

        private async Task LoadDoctors()
        {
            try
            {
                Wizard.Doctors =
                    (await _doctorService.GetDoctorsAsync(
                        FacilityId, SpecialityId ?? 0))?.Data ?? new();
                _logger.LogInformation(
                    "LoadDoctors | Count={C}", Wizard.Doctors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LoadDoctors failed | FacilityId={F} SpecialityId={S}",
                    FacilityId, SpecialityId);
                Wizard.Doctors = new();
            }
        }

        private async Task LoadDoctorDetails()
        {
            try
            {
                Wizard.SelectedDoctor = Wizard.Doctors
                    .FirstOrDefault(x => x.Id == (DoctorId ?? 0).ToString());

                if (Wizard.SelectedDoctor == null)
                {
                    _logger.LogWarning(
                        "Doctor not found | DoctorId={D}", DoctorId);
                    return;
                }

                var today = DateTime.Now.ToString("yyyy-MM-dd");

                var consultationTask = _consultationService.GetConsultationFeeAsync(FacilityId, (DoctorId ?? 0));

                var slotsTask = _slotService.GetSlotsAsync(FacilityId,(DoctorId ?? 0),Wizard.SelectedDoctor.HospitalLocationId,
                    today, today);

                await Task.WhenAll(consultationTask, slotsTask);

                var consultantResponse = await consultationTask;
                var slotResponse = await slotsTask;

                Wizard.ConsultationFeeList = consultantResponse?
                    .Where(x => x.ServiceType == "CL")
                    .ToList() ?? new();

                Wizard.Slots = slotResponse?.Data?.SlotsDetails
                    ?? new List<SlotResponse>();

                _logger.LogInformation(
                    "LoadDoctorDetails | Slots={S} Fees={F}",
                    Wizard.Slots.Count, Wizard.ConsultationFeeList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LoadDoctorDetails failed | DoctorId={D} FacilityId={F}",
                    DoctorId, FacilityId);
                Wizard.Slots = new();
                Wizard.ConsultationFeeList = new();
            }
        }

        // ── Helpers ──────────────────────────────────────────
        private static string GetInitials(string name) =>
            string.Concat(
                name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(x => char.ToUpper(x[0])));

        private static string FormatSlotDisplay(string slotTime)
        {
            if (DateTime.TryParse(slotTime, out var dt))
                return dt.ToString("ddd, d MMM · hh:mm tt");

            if (TimeSpan.TryParse(slotTime, out var time))
            {
                var date = DateTime.Now.ToString("ddd, d MMM");
                var timeStr = DateTime.Today.Add(time).ToString("hh:mm tt");
                return $"{date} · {timeStr}";
            }
            return slotTime;
        }
    }
}