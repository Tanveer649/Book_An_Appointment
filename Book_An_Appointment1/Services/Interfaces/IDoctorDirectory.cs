using Book_An_Appointment1.Models.DoctorItem;

namespace Book_An_Appointment1.Services.Interfaces
{
    // Doctor found for a public link (hospital website / doctor profile / shared URL)
    public record DoctorMatch(DoctorItem Doctor, int FacilityId, int SpecialityId, string FacilityName);

    public interface IDoctorDirectory
    {
        // facilityId optional: when missing, every facility of this portal is searched
        Task<DoctorMatch?> FindAsync(int doctorId, int? facilityId = null);

        // Consultation fee as on the booking page (Consultation API "CL" → doctorFee fallback); null = unknown
        Task<decimal?> ConsultationFeeAsync(int facilityId, DoctorItem doctor);
    }
}
