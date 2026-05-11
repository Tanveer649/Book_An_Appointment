using Book_An_Appointment1.Models.Consultation;
using Book_An_Appointment1.Models.DoctorItem;
using Book_An_Appointment1.Models.Slot;
using Book_An_Appointment1.Models.Speciality;
using Book_An_Appointment1.Services.Interfaces;
using Book_An_Appointment1.ViewModels.Appointment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Book_An_Appointment1.Pages.Appointment
{
    public class IndexModel : PageModel
    {
        private readonly IFacilityService _facilityService;
        private readonly ISpecialityService _specialityService;
        private readonly IDoctorService _doctorService;
        private readonly IConsultationService _consultationService;
        private readonly ISlotService _slotService;

        public AppointmentWizardViewModel Wizard { get; set; } = new();

        [BindProperty]
        public int FacilityId { get; set; }

        [BindProperty]
        public int SpecialityId { get; set; }

        [BindProperty]
        public int DoctorId { get; set; }

        public IndexModel(
            IFacilityService facilityService,
            ISpecialityService specialityService,
            IDoctorService doctorService,
            IConsultationService consultationService,
            ISlotService slotService)
        {
            _facilityService = facilityService;
            _specialityService = specialityService;
            _doctorService = doctorService;
            _consultationService = consultationService;
            _slotService = slotService;
        }

        public async Task OnGetAsync()
        {
            await LoadFacilities();

            //if (FacilityId > 0)
            //    await LoadSpecialities();

            //if (SpecialityId > 0)
            //    await LoadDoctors();

            //if (DoctorId > 0)
            //    await LoadDoctorDetails();
        }

        //public async Task<IActionResult> OnPostAsync()
        //{
        //    await LoadFacilities();

        //    if (FacilityId > 0)
        //        await LoadSpecialities();

        //    if (SpecialityId > 0)
        //        await LoadDoctors();

        //    if (DoctorId > 0)
        //        await LoadDoctorDetails();

        //    return Page();
        //}


        public async Task<IActionResult> OnPostAsync()
        {
            
            await LoadFacilities();
            Console.WriteLine($"RAW DoctorId: {DoctorId}");

            if (FacilityId > 0 && SpecialityId == 0 && DoctorId == 0)
            {
                // Facility select hua
                await LoadSpecialities();
                TempData["Specialities"] = JsonSerializer.Serialize(Wizard.Specialities);
            }
            else if (SpecialityId > 0 && DoctorId == 0)
            {
                // Speciality select hua
                await LoadDoctors();

                var specialitiesJson = TempData["Specialities"] as string;
                if (specialitiesJson != null)
                    Wizard.Specialities = JsonSerializer.Deserialize<List<SpecialityItem>>(specialitiesJson) ?? new();

                TempData["Specialities"] = specialitiesJson; // preserve karo
                TempData["Doctors"] = JsonSerializer.Serialize(Wizard.Doctors);
            }
            else if (SpecialityId > 0 && DoctorId > 0)
            {
                // Doctor select hua
                await LoadDoctors();
                await LoadDoctorDetails();

                var specialitiesJson = TempData["Specialities"] as string;
                if (specialitiesJson != null)
                    Wizard.Specialities = JsonSerializer.Deserialize<List<SpecialityItem>>(specialitiesJson) ?? new();

                TempData["Specialities"] = specialitiesJson; // preserve karo
            }

            return Page();
        }

        private async Task LoadFacilities()
        {
            if (Wizard.Facilities.Any()) return; // already loaded hai

            Wizard.Facilities =
                (await _facilityService.GetFacilitiesAsync())?.Data ?? new();
        }

        private async Task LoadSpecialities()
        {
            Wizard.Specialities =
                (await _specialityService
                .GetSpecialitiesAsync(FacilityId))?.Data ?? new();
        }

        private async Task LoadDoctors()
        {
            Wizard.Doctors =
                (await _doctorService
                .GetDoctorsAsync(FacilityId, SpecialityId))?.Data ?? new();
        }

        private async Task LoadDoctorDetails()
        {
            var doctors =
                (await _doctorService.GetDoctorsAsync(FacilityId, SpecialityId))?.Data ?? new();

            Wizard.SelectedDoctor = Wizard.Doctors.FirstOrDefault(x => x.Id == DoctorId.ToString());

            var consultantResponse = await _consultationService.GetConsultationFeeAsync(FacilityId, DoctorId);


            //var clPrice = consultantResponse?
            //    .SelectMany(x => x.Data)
            //    .FirstOrDefault(x => x.ServiceType == "CL")
            //    ?.Price;

            //Wizard.ConsultationFee = new ConsultationResponse
            //{
            //    Data = consultantResponse?
            //         .SelectMany(x => x.Data)
            //         .Where(x => x.ServiceType == "CL")
            //         .ToList() ?? new()
            //};

            Wizard.ConsultationFee = new ConsultationResponse
            {
                Data = consultantResponse?
             .SelectMany(x => x.Data)
             .Where(x => x.ServiceType == "CL")
             .ToList() ?? new()
            };

            var slotResponse =
                await _slotService.GetSlotsAsync(
                    FacilityId,
                    DoctorId,
                    Wizard.SelectedDoctor?.HospitalLocationId ?? 24);

            Wizard.Slots =
                slotResponse?.Data?.SlotsDetails ?? new List<SlotResponse>();
        }
    }
}