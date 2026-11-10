using Book_An_Appointment1.Models.Appointment;
using Book_An_Appointment1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace Book_An_Appointment1.Pages.Slip
{
    // Public appointment slip: /slip/{token} (page), ?handler=Pdf (download), ?handler=Ics (calendar file).
    // Opened from the "Appointment booked" popup or by scanning the QR code on another phone — no session needed.
    // The token is signed + time-limited (AppointmentSlipService), so only real booking links work.
    public class SlipModel : PageModel
    {
        private readonly IAppointmentSlipService _slipService;
        private readonly ILogger<SlipModel> _logger;

        public SlipModel(IAppointmentSlipService slipService, ILogger<SlipModel> logger, IConfiguration configuration)
        {
            _slipService = slipService;
            _logger = logger;
            CalendarEnabled = configuration.GetValue("SlipSettings:EnableAddToCalendar", false);
            PrintEnabled = configuration.GetValue("SlipSettings:EnablePrint", false);
        }

        // appsettings SlipSettings:EnablePrint — Print button (popup + slip page) and ?print=1
        public bool PrintEnabled { get; }

        // appsettings SlipSettings:EnableAddToCalendar — Google Calendar link + .ics download
        public bool CalendarEnabled { get; }

        public AppointmentSlip? Slip { get; private set; }
        public string Token { get; private set; } = "";
        public string SlipUrl { get; private set; } = "";
        public string QrSvg { get; private set; } = "";
        public string? GoogleCalendarUrl { get; private set; }
        public bool LinkInvalid { get; private set; }
        public bool AutoPrint { get; private set; }

        public async Task<IActionResult> OnGetAsync(string token, bool print = false)
        {
            NoStore();
            var ticket = _slipService.ReadToken(token);
            if (ticket == null)
            {
                _logger.LogWarning("Slip link invalid or expired");
                LinkInvalid = true;
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Page();
            }

            Token = token;
            SlipUrl = _slipService.SlipUrl(Request, token);
            Slip = await _slipService.GetSlipAsync(ticket);
            if (Slip != null)
            {
                QrSvg = _slipService.QrSvg(SlipUrl);
                GoogleCalendarUrl = CalendarEnabled ? _slipService.GoogleCalendarUrl(Slip, SlipUrl) : null;
                AutoPrint = print && PrintEnabled;
            }
            return Page();
        }

        public async Task<IActionResult> OnGetPdfAsync(string token)
        {
            NoStore();
            var (slip, url) = await LoadAsync(token);
            if (slip == null) return NotFound();

            var pdf = _slipService.BuildPdf(slip, url);
            if (pdf == null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
            return File(pdf, "application/pdf", $"Appointment-{slip.AppointmentId}.pdf");
        }

        public async Task<IActionResult> OnGetIcsAsync(string token)
        {
            NoStore();
            if (!CalendarEnabled) return NotFound();
            var (slip, url) = await LoadAsync(token);
            if (slip == null || slip.StartsAt == null) return NotFound();

            var ics = _slipService.BuildIcs(slip, url, Request.Host.Host);
            return File(Encoding.UTF8.GetBytes(ics), "text/calendar; charset=utf-8", $"Appointment-{slip.AppointmentId}.ics");
        }

        private async Task<(AppointmentSlip? Slip, string Url)> LoadAsync(string token)
        {
            var ticket = _slipService.ReadToken(token);
            if (ticket == null) return (null, "");
            return (await _slipService.GetSlipAsync(ticket), _slipService.SlipUrl(Request, token));
        }

        // Patient data: never cached by browsers / proxies, never indexed by search engines
        private void NoStore()
        {
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        }
    }
}
