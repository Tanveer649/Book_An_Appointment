using Book_An_Appointment1.Models.Consultation;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface IConsultationService
    {
        Task<List<ConsultationResponse>?> GetConsultationFeeAsync(
            int facilityId,
            int doctorId);
    }
}
