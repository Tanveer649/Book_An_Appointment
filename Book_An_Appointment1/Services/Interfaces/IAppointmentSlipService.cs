using Book_An_Appointment1.Models.Appointment;

namespace Book_An_Appointment1.Services.Interfaces
{
    public interface IAppointmentSlipService
    {
        // Signed, time-limited link value for /slip/{token}
        string CreateToken(SlipTicket ticket);
        SlipTicket? ReadToken(string? token);

        // Slip details (GetAppointmentByID, cached briefly); null when the API has no record
        Task<AppointmentSlip?> GetSlipAsync(SlipTicket ticket);

        // Absolute address of the slip page — what the QR code opens
        string SlipUrl(HttpRequest request, string token);

        string QrSvg(string text);
        byte[]? BuildPdf(AppointmentSlip slip, string slipUrl);
        string BuildIcs(AppointmentSlip slip, string slipUrl, string host);
        string? GoogleCalendarUrl(AppointmentSlip slip, string slipUrl);
    }
}
