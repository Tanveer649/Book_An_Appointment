namespace Book_An_Appointment1.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();
        void ClearToken();
    }
}