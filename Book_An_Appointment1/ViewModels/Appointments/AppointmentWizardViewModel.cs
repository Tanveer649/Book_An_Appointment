using Book_An_Appointment1.Models.Consultation;
using Book_An_Appointment1.Models.DoctorItem;
using Book_An_Appointment1.Models.Facility;
using Book_An_Appointment1.Models.Slot;
using Book_An_Appointment1.Models.Speciality;

namespace Book_An_Appointment1.ViewModels.Appointment
{
    public class AppointmentWizardViewModel
    {
        public List<FacilityItem> Facilities { get; set; } = new();
        public List<SpecialityItem> Specialities { get; set; } = new();
        public List<DoctorItem> Doctors { get; set; } = new();

        //public ConsultationResponse ConsultationFee { get; set; } = new();
        public List<SlotResponse> Slots { get; set; } = new();

        public int SelectedFacilityId { get; set; }
        public int SelectedSpecialityId { get; set; }
        public int SelectedDoctorId { get; set; }

        public DoctorItem? SelectedDoctor { get; set; }

        //public string ConsultationAmount =>
        //    ConsultationFee.Data.FirstOrDefault()?.Price ?? "N/A";
        public List<ConsultationItem> ConsultationFeeList { get; set; } = new();

        public string ConsultationAmount =>
            ConsultationFeeList?.FirstOrDefault()?.Price ?? "N/A";
        public string? ErrorMessage { get; set; }
    }

    
}