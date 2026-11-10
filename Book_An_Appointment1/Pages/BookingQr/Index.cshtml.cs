using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using System.Text;

namespace Book_An_Appointment1.Pages.BookingQr
{
    // Wall poster: QR code that opens the booking portal (/booking-qr).
    // Print it, or download the QR as PNG (print shops / designers) or SVG (sharp at any size).
    // appsettings BookingQrSettings: Enabled, Url (what the QR opens), Title, Subtitle.

    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration) => _configuration = configuration;

        public string BookingUrl { get; private set; } = "";
        public string QrSvg { get; private set; } = "";
        public string? Title { get; private set; }
        public string? Subtitle { get; private set; }

        public IActionResult OnGet()
        {
            if (!Enabled) return NotFound();
            BookingUrl = ResolveUrl();
            QrSvg = Svg(BookingUrl);
            Title = Text("BookingQrSettings:Title");
            Subtitle = Text("BookingQrSettings:Subtitle");
            Response.Headers["X-Robots-Tag"] = "noindex";
            return Page();
        }

        // High resolution PNG (≈ 1500 px) for printing / designers
        public IActionResult OnGetPng()
        {
            if (!Enabled) return NotFound();
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(ResolveUrl(), QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data).GetGraphic(40);
            return File(png, "image/png", "booking-qr.png");
        }

        public IActionResult OnGetSvg()
        {
            if (!Enabled) return NotFound();
            return File(Encoding.UTF8.GetBytes(Svg(ResolveUrl())), "image/svg+xml", "booking-qr.svg");
        }

        private bool Enabled => _configuration.GetValue("BookingQrSettings:Enabled", false);

        // What the QR opens: BookingQrSettings:Url → SlipSettings:PublicBaseUrl + /book → this site + /book
        private string ResolveUrl()
        {
            var url = _configuration["BookingQrSettings:Url"]?.Trim();
            if (!string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute)) return url;

            var baseUrl = _configuration["SlipSettings:PublicBaseUrl"]?.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl) || !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
                baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            return baseUrl + Security.BookingFlow.BasePath;
        }

        private static string Svg(string text)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);   // Q: still scans if the poster gets a little damaged
            return new SvgQRCode(data).GetGraphic(10, "#000000", "#ffffff", true, SvgQRCode.SizingMode.ViewBoxAttribute);
        }

        private string? Text(string key)
        {
            var v = _configuration[key]?.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }
    }
}
