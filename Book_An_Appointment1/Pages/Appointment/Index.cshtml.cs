using Book_An_Appointment1.Models.Consultation;
using Book_An_Appointment1.Models.Slot;
using Book_An_Appointment1.Services.Interfaces;
using Book_An_Appointment1.ViewModels.Appointment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Book_An_Appointment1.Pages.Appointment
{
    public class IndexModel : PageModel
    {
        private readonly IFacilityService _facilityService;
        private readonly ISpecialityService _specialityService;
        private readonly IDoctorService _doctorService;
        private readonly IConsultationService _consultationService;
        private readonly ISlotService _slotService;
        private readonly ILogger<IndexModel> _logger;

        public AppointmentWizardViewModel Wizard { get; set; } = new();

        [BindProperty] public int FacilityId { get; set; }
        [BindProperty] public int? SpecialityId { get; set; }
        [BindProperty] public int? DoctorId { get; set; }
        [BindProperty] public string? ErrorMessage { get; set; }

        public IndexModel(
            IFacilityService facilityService,
            ISpecialityService specialityService,
            IDoctorService doctorService,
            IConsultationService consultationService,
            ISlotService slotService,
            ILogger<IndexModel> logger)
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
            await LoadFacilities();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();

            // Parallel mein facilities load karo — hamesha chahiye

            var facilitiesTask = LoadFacilities();

            if (FacilityId > 0 && (SpecialityId ?? 0) == 0 && (DoctorId ?? 0) == 0)
            {
                // Facility select hua — facilities + specialities parallel load karo
                await Task.WhenAll(facilitiesTask, LoadSpecialities());
            }
            else if (FacilityId > 0 && (SpecialityId ?? 0) > 0 && (DoctorId ?? 0) == 0)
            {
                // Speciality select hua — facilities + doctors parallel load karo
                await Task.WhenAll(facilitiesTask, LoadSpecialities(), LoadDoctors());
            }
            else if (FacilityId > 0 && (SpecialityId ?? 0) > 0 && (DoctorId ?? 0) > 0)
            {
                // Doctor select hua — sab parallel load karo
                await facilitiesTask;
                await Task.WhenAll(LoadSpecialities(), LoadDoctors());
                await LoadDoctorDetails(); // Doctors load hone ke baad
            }
            else
            {
                await facilitiesTask;
            }

            return Page();

        }

        public async Task<JsonResult> OnGetSlotsByDateAsync(int facilityId, int? doctorId, int hospitalLocationId, string date)
        {
            try
            {
                var slotResponse = await _slotService.GetSlotsAsync(facilityId, (doctorId ?? 0), hospitalLocationId, date, date);

                var slots = slotResponse?.Data?.SlotsDetails ?? new List<SlotResponse>();
                return new JsonResult(slots);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
               "OnGetSlotsByDateAsync failed | FacilityId={FacilityId} DoctorId={DoctorId} Date={Date}",
               facilityId, doctorId, date);

                return new JsonResult(new
                {
                    ErrorMessage = ex.Message,
                    slots = new List<SlotResponse>()
                });
            }
        }

        private async Task LoadFacilities()
        {
            try
            {
                Wizard.Facilities = (await _facilityService.GetFacilitiesAsync())?.Data ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadFacilities failed");
                ErrorMessage = ex.Message;
                Wizard.Facilities = new();
            }
        }

        private async Task LoadSpecialities()
        {
            try
            {
                Wizard.Specialities =
                    (await _specialityService.GetSpecialitiesAsync(FacilityId))?.Data ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
               "LoadSpecialities failed | FacilityId={FacilityId}", FacilityId);
                ErrorMessage = ex.Message;
                Wizard.Specialities = new();
            }
        }

        private async Task LoadDoctors()
        {
            try
            {
                Wizard.Doctors =
                    (await _doctorService.GetDoctorsAsync(FacilityId, SpecialityId ?? 0))?.Data ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LoadDoctors failed | FacilityId={FacilityId} SpecialityId={SpecialityId}",
                FacilityId, SpecialityId);
                ErrorMessage = ex.Message;
                Wizard.Doctors = new();
            }
        }

        private async Task LoadDoctorDetails()
        {
            try
            {
                // Doctors already loaded hain — extra API call nahi
                Wizard.SelectedDoctor = Wizard.Doctors
                    .FirstOrDefault(x => x.Id == (DoctorId ?? 0).ToString());

                if (Wizard.SelectedDoctor == null) return;

                var today = DateTime.Now.ToString("yyyy-MM-dd");

                // Consultation fee aur slots parallel load karo
                var consultationTask =
                    _consultationService.GetConsultationFeeAsync(FacilityId, (DoctorId ?? 0));

                var slotsTask = _slotService.GetSlotsAsync(
                    FacilityId,
                    (DoctorId ?? 0),
                    Wizard.SelectedDoctor.HospitalLocationId,
                    today,
                    today);

                await Task.WhenAll(consultationTask, slotsTask);

                var consultantResponse = await consultationTask;
                var slotResponse = await slotsTask;

                Wizard.ConsultationFeeList = consultantResponse?
                    .Where(x => x.ServiceType == "CL")
                    .ToList() ?? new();

                Wizard.Slots = slotResponse?.Data?.SlotsDetails
                    ?? new List<SlotResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "LoadDoctorDetails failed | DoctorId={DoctorId} FacilityId={FacilityId}",
                    DoctorId, FacilityId);

                ErrorMessage = ex.Message;

                Wizard.Slots = new();
                Wizard.ConsultationFeeList = new();
            }
        }

        public async Task<IActionResult> OnPostContinueAsync(string selectedSlotStart, string selectedSlotEnd)
        {
            ModelState.Clear();

            // Server side validation — sirf slot
            if (string.IsNullOrEmpty(selectedSlotStart))
                ModelState.AddModelError("Slot", "Please select a time slot.");

            if (!ModelState.IsValid)
            {
                await LoadFacilities();
                await Task.WhenAll(LoadSpecialities(), LoadDoctors());
                await LoadDoctorDetails();
                return Page();
            }

            // Valid — next page pe jayenge (baad mein session add karenge)
            return RedirectToPage("/Appointment/PatientDetails");
        }
    }
}