using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Models.Appointment;
using Book_An_Appointment1.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using QRCoder;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Book_An_Appointment1.Services.Implementations
{
    // Appointment slip after a "Pay on arrival" booking: web page /slip/{token}, PDF, QR code and calendar entry.
    // The token is encrypted + signed by ASP.NET Data Protection and expires (SlipSettings:LinkValidDays),
    // so a slip link cannot be guessed, changed to another appointment ID, or read.
    public class AppointmentSlipService : IAppointmentSlipService
    {
        private const string Purpose = "AppointmentSlip.v1";
        private const char Separator = '\u001F';

        private readonly ITimeLimitedDataProtector _protector;
        private readonly IAppointmentBookingService _bookingService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ThemeProvider _theme;
        private readonly ILogger<AppointmentSlipService> _logger;

        public AppointmentSlipService(IDataProtectionProvider dataProtection, IAppointmentBookingService bookingService,
            IMemoryCache cache, IConfiguration configuration, ThemeProvider theme, ILogger<AppointmentSlipService> logger)
        {
            _protector = dataProtection.CreateProtector(Purpose).ToTimeLimitedDataProtector();
            _bookingService = bookingService;
            _cache = cache;
            _configuration = configuration;
            _theme = theme;
            _logger = logger;
        }

        // ── Link ─────────────────────────────────────────────
        public string CreateToken(SlipTicket ticket)
        {
            var days = Math.Clamp(_configuration.GetValue("SlipSettings:LinkValidDays", 30), 1, 365);
            var payload = string.Join(Separator, ticket.AppointmentId, ticket.Amount, MaskMobile(ticket.Mobile), ticket.PaymentMode,
                ticket.Uhid?.Trim() ?? "", ticket.PatientName?.Trim() ?? "");
            return _protector.Protect(payload, TimeSpan.FromDays(days));
        }

        public SlipTicket? ReadToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 600) return null;
            try
            {
                var parts = _protector.Unprotect(token).Split(Separator);
                // 6 parts; older links have 4 (no UHID) or 5 (no patient name)
                if (parts.Length is < 4 or > 6 || !long.TryParse(parts[0], out var id) || id <= 0) return null;
                return new SlipTicket(parts[0], parts[1], parts[2], parts[3], parts.Length >= 5 ? parts[4] : "", parts.Length == 6 ? parts[5] : "");
            }
            catch (CryptographicException)
            {
                return null;   // changed, from another server's keys, or expired
            }
        }

        public string SlipUrl(HttpRequest request, string token)
        {
            // Behind a proxy / load balancer the public address can differ from what the app sees
            var baseUrl = _configuration["SlipSettings:PublicBaseUrl"]?.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl) || !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
                baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            return $"{baseUrl}/slip/{token}";
        }

        // ── Data ─────────────────────────────────────────────
        public async Task<AppointmentSlip?> GetSlipAsync(SlipTicket ticket)
        {
            var key = "slip:" + ticket.AppointmentId;
            if (_cache.TryGetValue(key, out AppointmentSlip? cached) && cached != null)
                return WithTicket(cached, ticket);

            try
            {
                var result = await _bookingService.GetAppointmentByIdAsync(ticket.AppointmentId);
                var item = result?.Data?.FirstOrDefault();
                if (!string.Equals(result?.Status, "success", StringComparison.OrdinalIgnoreCase) || item == null)
                {
                    _logger.LogWarning("Slip — GetAppointmentByID no record | AppointmentId={A} Message={M}", ticket.AppointmentId, result?.Message);
                    return null;
                }

                var slip = new AppointmentSlip
                {
                    AppointmentId = ticket.AppointmentId,
                    PatientName = Clean(item.PatientName),
                    RegistrationNo = item.RegistrationNo?.Trim(),
                    DoctorName = Clean(item.DoctorName),
                    Specialisation = Clean(item.SpecializationName),
                    HospitalName = Clean(item.FacilityName),
                    StartsAt = ParseDate(item.AppStartTime),
                    EndsAt = ParseDate(item.AppToTime),
                    VisitType = item.VisitType?.Trim(),
                    Status = item.Status?.Trim(),
                    PaymentStatus = item.PaymentStatus?.Trim(),
                    Remarks = item.Remarks?.Trim()
                };

                _cache.Set(key, slip, TimeSpan.FromMinutes(5));
                return WithTicket(slip, ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Slip — GetAppointmentByID failed | AppointmentId={A}", ticket.AppointmentId);
                return null;
            }
        }

        private static AppointmentSlip WithTicket(AppointmentSlip s, SlipTicket t) => new()
        {
            AppointmentId = s.AppointmentId,
            PatientName = !string.IsNullOrWhiteSpace(s.PatientName) ? s.PatientName : t.PatientName,
            RegistrationNo = !string.IsNullOrWhiteSpace(s.RegistrationNo) ? s.RegistrationNo : NullIfEmpty(t.Uhid),
            Mobile = t.Mobile,
            DoctorName = s.DoctorName,
            Specialisation = s.Specialisation,
            HospitalName = s.HospitalName,
            StartsAt = s.StartsAt,
            EndsAt = s.EndsAt,
            VisitType = s.VisitType,
            Status = s.Status,
            PaymentStatus = s.PaymentStatus,
            Remarks = s.Remarks,
            Amount = t.Amount,
            PaymentMode = t.PaymentMode
        };

        // ── QR code ──────────────────────────────────────────
        public string QrSvg(string text)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
            var svg = new SvgQRCode(data);
            // viewBox sizing → the image scales with its CSS box
            return svg.GetGraphic(8, "#000000", "#ffffff", true, SvgQRCode.SizingMode.ViewBoxAttribute);
        }

        // ── PDF ──────────────────────────────────────────────
        public byte[]? BuildPdf(AppointmentSlip slip, string slipUrl)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(slipUrl, QRCodeGenerator.ECCLevel.M);

            return SlipPdfBuilder.Build(slip, data.ModuleMatrix, new SlipPdfBuilder.Options
            {
                PortalName = _configuration["HospitalSettings:HospitalName"]?.Trim() ?? "",
                BrandColorHex = _theme.PrimaryColor,
                ArrivalMinutes = _configuration.GetValue("SlipSettings:ArriveMinutesBefore", 15),
                FontRegular = _configuration["SlipSettings:PdfFontRegular"],
                FontBold = _configuration["SlipSettings:PdfFontBold"]
            }, _logger);
        }

        // ── Calendar ─────────────────────────────────────────
        public string BuildIcs(AppointmentSlip slip, string slipUrl, string host)
        {
            var (start, end) = UtcTimes(slip);
            var reminder = Math.Clamp(_configuration.GetValue("SlipSettings:ReminderMinutesBefore", 60), 0, 10080);
            var hospital = !string.IsNullOrWhiteSpace(slip.HospitalName) ? slip.HospitalName : _configuration["HospitalSettings:HospitalName"] ?? "";

            var lines = new List<string>
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//Book Appointment Portal//Appointment Slip//EN",
                "CALSCALE:GREGORIAN",
                "METHOD:PUBLISH",
                "BEGIN:VEVENT",
                $"UID:appointment-{slip.AppointmentId}@{IcsText(host)}",
                $"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}",
                $"DTSTART:{start:yyyyMMdd'T'HHmmss'Z'}",
                $"DTEND:{end:yyyyMMdd'T'HHmmss'Z'}",
                $"SUMMARY:{IcsText(EventTitle(slip))}",
                $"LOCATION:{IcsText(hospital)}",
                $"DESCRIPTION:{IcsText(EventDetails(slip, slipUrl))}",
                $"URL:{slipUrl}"
            };
            if (reminder > 0)
            {
                lines.Add("BEGIN:VALARM");
                lines.Add($"TRIGGER:-PT{reminder}M");
                lines.Add("ACTION:DISPLAY");
                lines.Add($"DESCRIPTION:{IcsText(EventTitle(slip))}");
                lines.Add("END:VALARM");
            }
            lines.Add("END:VEVENT");
            lines.Add("END:VCALENDAR");

            return string.Join("\r\n", lines.Select(Fold)) + "\r\n";
        }

        public string? GoogleCalendarUrl(AppointmentSlip slip, string slipUrl)
        {
            if (slip.StartsAt == null) return null;
            var (start, end) = UtcTimes(slip);
            var hospital = !string.IsNullOrWhiteSpace(slip.HospitalName) ? slip.HospitalName : _configuration["HospitalSettings:HospitalName"] ?? "";

            return "https://calendar.google.com/calendar/render?action=TEMPLATE" +
                   "&text=" + Uri.EscapeDataString(EventTitle(slip)) +
                   "&dates=" + start.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture) + "/" +
                               end.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture) +
                   "&details=" + Uri.EscapeDataString(EventDetails(slip, slipUrl)) +
                   "&location=" + Uri.EscapeDataString(hospital);
        }

        // Appointment times are hospital-local (SlipSettings:TimeZone) → UTC for calendars
        private (DateTime Start, DateTime End) UtcTimes(AppointmentSlip slip)
        {
            var zone = HospitalTimeZone();
            var localStart = DateTime.SpecifyKind(slip.StartsAt ?? DateTime.Now, DateTimeKind.Unspecified);
            var localEnd = slip.EndsAt is { } e && e > localStart
                ? DateTime.SpecifyKind(e, DateTimeKind.Unspecified)
                : localStart.AddMinutes(_configuration.GetValue("AppointmentApi:DefaultSlotMinutes", 15));
            return (TimeZoneInfo.ConvertTimeToUtc(localStart, zone), TimeZoneInfo.ConvertTimeToUtc(localEnd, zone));
        }

        private TimeZoneInfo HospitalTimeZone()
        {
            var id = _configuration["SlipSettings:TimeZone"]?.Trim();
            if (string.IsNullOrEmpty(id)) id = "Asia/Kolkata";
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                _logger.LogWarning("SlipSettings:TimeZone '{Zone}' not found — using the server time zone", id);
                return TimeZoneInfo.Local;
            }
        }

        private static string EventTitle(AppointmentSlip s) =>
            string.IsNullOrWhiteSpace(s.DoctorName) ? "Doctor appointment" : $"Appointment with {s.DoctorName}";

        private static string EventDetails(AppointmentSlip s, string slipUrl)
        {
            var sb = new StringBuilder();
            sb.Append("Appointment ID: ").Append(s.AppointmentId).Append('\n');
            if (!string.IsNullOrWhiteSpace(s.PatientName)) sb.Append("Patient: ").Append(s.PatientName).Append('\n');
            if (!string.IsNullOrWhiteSpace(s.Specialisation)) sb.Append("Speciality: ").Append(s.Specialisation).Append('\n');
            sb.Append("Show your appointment slip at the registration counter.\n");
            sb.Append("Slip: ").Append(slipUrl);
            return sb.ToString();
        }

        // RFC 5545 text escaping
        private static string IcsText(string? value) =>
            (value ?? "").Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r\n", "\\n").Replace("\n", "\\n");

        // Lines longer than 75 characters are folded (continuation lines start with a space)
        private static string Fold(string line)
        {
            if (line.Length <= 75) return line;
            var sb = new StringBuilder(line[..75]);
            for (var i = 75; i < line.Length; i += 74)
                sb.Append("\r\n ").Append(line.AsSpan(i, Math.Min(74, line.Length - i)));
            return sb.ToString();
        }

        // ── Helpers ──────────────────────────────────────────
        private static string MaskMobile(string? mobile)
        {
            var m = (mobile ?? "").Trim();
            if (m.Contains('X')) return m;                              // already masked
            return m.Length > 4 ? new string('X', m.Length - 4) + m[^4..] : m;
        }

        private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

        private static string Clean(string? value) =>
            string.Join(" ", (value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
