using Book_An_Appointment1.Models.PatientTitle;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface ITitleService
    {
        Task<TitleResponse?> GetTitlesAsync();
    }
}
