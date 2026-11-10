using Book_An_Appointment1.Models.Patient;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface IPatientRegistrationService
    {
        // Registers a family member under a verified mobile (MobSaveRegistrationForPatient)
        Task<FamilyMemberRegistrationResult> SaveFamilyMemberAsync(FamilyMemberRegistrationRequest request);
    }
}
