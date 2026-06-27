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
        public string? SavedSlotDate { get; set; }
        public string? SavedSlotStart { get; set; }
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

        public async Task OnGetAsync()
        {
            _logger.LogInformation("OnGetAsync started");

            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session);

            await LoadFacilities();

            if (sessionData != null && sessionData.FacilityId > 0)
            {
                FacilityId = sessionData.FacilityId;
                SpecialityId = sessionData.SpecialityId > 0 ? sessionData.SpecialityId : null;
                DoctorId = sessionData.DoctorId > 0 ? sessionData.DoctorId : null;
                SavedSlotDate = sessionData.SlotDate;
                SavedSlotStart = sessionData.SlotTime;

                await LoadSpecialities();

                if (SpecialityId.HasValue && SpecialityId > 0)
                {
                    await LoadDoctors();

                    if (DoctorId.HasValue && DoctorId > 0)
                    {
                        await LoadDoctorDetails();

                        var dateToLoad = !string.IsNullOrEmpty(sessionData.SlotDate) ? sessionData.SlotDate : DateTime.Now.ToString("yyyy-MM-dd");

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
                var feeRaw = Wizard.ConsultationFeeList?.FirstOrDefault()?.Price
                               ?? Wizard.ConsultationFeeList?.FirstOrDefault()?.NetAmount
                               ?? "0";
                // Trailing zeros remove karo
                var fee = decimal.TryParse(feeRaw, out var feeDecimal)
                          ? ((int)feeDecimal).ToString()
                          : feeRaw;

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
                var slots = await GetDoctorSlotsAsync(facilityId, doctorId ?? 0, hospitalLocationId, date);

                return new JsonResult(slots);

                //var slotResponse = await _slotService.GetSlotsAsync(facilityId, (doctorId ?? 0), hospitalLocationId, date, date);
                //var slots = slotResponse?.Data?.SlotsDetails ?? new List<SlotResponse>();
                //return new JsonResult(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnGetSlotsByDateAsync failed | FacilityId={F} DoctorId={D} Date={Date}",
                    facilityId, doctorId, date);
                return new JsonResult(new List<SlotResponse>());
            }
        }

        // ── POST: Continue Button ────────────────────────────
        // MODIFIED: Validation fail hone pe ab poora Wizard reload karta hai
        // taaki dropdowns/doctor card UI mein blank na dikhein
        public async Task<IActionResult> OnPostContinueAsync(string selectedSlotStart, string selectedSlotEnd,
            int facilityId, int? specialityId, int? doctorId)
        {
            _logger.LogInformation("OnPostContinueAsync | Slot={Slot} FacilityId={F} SpecialityId={S} DoctorId={D}",
                selectedSlotStart, facilityId, specialityId, doctorId);

            ModelState.Clear();

            FacilityId = facilityId;
            SpecialityId = specialityId;
            DoctorId = doctorId;

            // ── Server Side Validation ─────────────────────
            if (facilityId <= 0)
                ModelState.AddModelError("Facility", "Please select a facility.");

            if (!specialityId.HasValue || specialityId <= 0)
                ModelState.AddModelError("Speciality", "Please select a speciality.");

            if (!doctorId.HasValue || doctorId <= 0)
                ModelState.AddModelError("Doctor", "Please select a doctor.");

            if (string.IsNullOrEmpty(selectedSlotStart))
                ModelState.AddModelError("Slot", "Please select a time slot.");

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

            // ── Session Save ──────────────────────────────
            var sessionData = SessionHelper.GetAppointmentData(HttpContext.Session)
                              ?? new AppointmentSessionData();

            sessionData.SlotTime = selectedSlotStart;
            sessionData.SlotDate = sessionData.SlotDate = DateTime.TryParse(selectedSlotStart, out var parsedSlot)
                       ? parsedSlot.ToString("yyyy-MM-dd")
                       : DateTime.Now.ToString("yyyy-MM-dd");
            sessionData.SlotDisplayText = FormatSlotDisplay(selectedSlotStart);

            SessionHelper.SetAppointmentData(HttpContext.Session, sessionData);

            _logger.LogInformation("Session slot saved | SlotDisplayText={S}", sessionData.SlotDisplayText);

            return RedirectToPage("/PatientDetails/PatientDetails");
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
                //Wizard.SelectedDoctor = Wizard.Doctors.FirstOrDefault(x => x.Id == (DoctorId ?? 0).ToString());

                //if (Wizard.SelectedDoctor == null)
                //{
                //    _logger.LogWarning("Doctor not found | DoctorId={D}", DoctorId);
                //    return;
                //}

                //var today = DateTime.Now.ToString("yyyy-MM-dd");

                //var consultationTask = _consultationService.GetConsultationFeeAsync(FacilityId, (DoctorId ?? 0));
                //var slotsTask = _slotService.GetSlotsAsync(FacilityId, (DoctorId ?? 0), Wizard.SelectedDoctor.HospitalLocationId, today, today);

                //await Task.WhenAll(consultationTask, slotsTask);

                //var consultantResponse = await consultationTask;
                //var slotResponse = await slotsTask;

                //Wizard.ConsultationFeeList = consultantResponse?.Where(x => x.ServiceType == "CL").ToList() ?? new();
                //Wizard.Slots = slotResponse?.Data?.SlotsDetails ?? new List<SlotResponse>();

                //_logger.LogInformation("LoadDoctorDetails | Slots={S} Fees={F}",
                //    Wizard.Slots.Count, Wizard.ConsultationFeeList.Count);


                Wizard.SelectedDoctor = Wizard.Doctors.FirstOrDefault(x => x.Id == (DoctorId ?? 0).ToString());

                if (Wizard.SelectedDoctor == null)
                {
                    _logger.LogWarning("Doctor not found | DoctorId={D}", DoctorId);
                    return;
                }

                var today = DateTime.Now.ToString("yyyy-MM-dd");

                var consultationTask = _consultationService.GetConsultationFeeAsync(FacilityId, (DoctorId ?? 0));
                var slotsTask = GetDoctorSlotsAsync(FacilityId, DoctorId ?? 0, Wizard.SelectedDoctor.HospitalLocationId, today); 

                await Task.WhenAll(consultationTask, slotsTask);  

                Wizard.Slots = await slotsTask;
                var consultantResponse = await consultationTask;

                Wizard.ConsultationFeeList = consultantResponse?.Where(x => x.ServiceType == "CL").ToList() ?? new();

                _logger.LogInformation("LoadDoctorDetails | Slots={S} Fees={F}",
                    Wizard.Slots.Count, Wizard.ConsultationFeeList.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadDoctorDetails failed | DoctorId={D} FacilityId={F}", DoctorId, FacilityId);
                Wizard.Slots = new();
                Wizard.ConsultationFeeList = new();
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