using Book_An_Appointment1.Models.DoctorItem;
using Book_An_Appointment1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Book_An_Appointment1.Pages.WebsiteDoctorLink
{
    // Public, shareable doctor profile: /doctor/{doctorId}  (optional ?facilityId=)
    // "Book appointment" → /book/doctor/{doctorId} (booking page with the doctor already selected).
    public class DoctorProfileModel : PageModel
    {
        private readonly IDoctorDirectory _directory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DoctorProfileModel> _logger;

        public DoctorProfileModel(IDoctorDirectory directory, IHttpClientFactory httpClientFactory, ILogger<DoctorProfileModel> logger)
        {
            _directory = directory;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public DoctorMatch? Match { get; private set; }
        public DoctorItem? Doctor => Match?.Doctor;
        public decimal? Fee { get; private set; }
        public string PhotoUrl { get; private set; } = "";
        public string BookUrl { get; private set; } = "";
        public string ProfileUrl { get; private set; } = "";
        public string? NextAvailable { get; private set; }
        public string? Designation { get; private set; }
        public List<string> Education { get; private set; } = new();
        public List<string> Treatments { get; private set; } = new();
        public List<string> Awards { get; private set; } = new();
        public string? About { get; private set; }

        public async Task<IActionResult> OnGetAsync(int doctorId, int? facilityId)
        {
            Match = await _directory.FindAsync(doctorId, facilityId);
            if (Match == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return Page();
            }

            var d = Match.Doctor;
            Fee = await _directory.ConsultationFeeAsync(Match.FacilityId, d);

            var query = $"?facilityId={Match.FacilityId}";
            BookUrl = Url.Content($"~/book/doctor/{d.DoctorId}") + query;
            ProfileUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/doctor/{d.DoctorId}{query}";

            // http photos are passed through this site (mixed content would be blocked on https)
            var icon = d.Icon?.Trim();
            PhotoUrl = string.IsNullOrEmpty(icon) ? ""
                : icon.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? icon
                : Url.Content($"~/doctor/{d.DoctorId}") + query + "&handler=Photo";

            Designation = Meaningful(d.Designation);
            Education = SplitList(d.Education);
            Treatments = SplitList(d.Treatment);
            Awards = SplitList(d.Awards);
            About = Meaningful(d.AboutUS) ?? Meaningful(d.ProfessionalStatement);

            if (DateTime.TryParse(d.Availability, CultureInfo.InvariantCulture, DateTimeStyles.None, out var next) && next >= DateTime.Today)
                NextAvailable = next.ToString("ddd, d MMM · hh:mm tt");

            return Page();
        }

        // Doctor photo stored on an http address → served from here (only this doctor's own photo)
        public async Task<IActionResult> OnGetPhotoAsync(int doctorId, int? facilityId)
        {
            var match = await _directory.FindAsync(doctorId, facilityId);
            if (match == null || !Uri.TryCreate(match.Doctor.Icon?.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return NotFound();

            try
            {
                using var resp = await _httpClientFactory.CreateClient("DoctorImageClient").GetAsync(uri);
                var type = resp.Content.Headers.ContentType?.MediaType ?? "";
                if (!resp.IsSuccessStatusCode || !type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return NotFound();
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                if (bytes.Length > 2_000_000) return NotFound();
                Response.Headers.CacheControl = "public,max-age=86400";
                return File(bytes, type);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Doctor profile photo fetch failed | DoctorId={D}", doctorId);
                return NotFound();
            }
        }

        // API placeholders such as "Select" / "N/A" are not shown
        private static string? Meaningful(string? value)
        {
            var v = value?.Trim();
            return string.IsNullOrEmpty(v) || v.Equals("select", StringComparison.OrdinalIgnoreCase) ||
                   v.Equals("n/a", StringComparison.OrdinalIgnoreCase) || v == "-" ? null : v;
        }

        private static List<string> SplitList(string? value) =>
            (Meaningful(value) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
