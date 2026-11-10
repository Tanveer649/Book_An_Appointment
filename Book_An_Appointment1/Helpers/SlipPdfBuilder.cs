using Book_An_Appointment1.Models.Appointment;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System.Collections;
using System.Globalization;

namespace Book_An_Appointment1.Helpers
{
    // A4 appointment slip as a real PDF (PDFsharp, MIT licence). Text is English — the slip is read
    // at the hospital counter; the web slip page itself follows the portal language.
    public static class SlipPdfBuilder
    {
        public class Options
        {
            public string PortalName { get; set; } = "";
            public string BrandColorHex { get; set; } = "#0F5C93";
            public int ArrivalMinutes { get; set; } = 15;
            public string? FontRegular { get; set; }   // optional font file paths (SlipSettings:PdfFontRegular / PdfFontBold)
            public string? FontBold { get; set; }
        }

        private const double PageW = 595, Margin = 40, ContentW = PageW - 2 * Margin;

        public static byte[]? Build(AppointmentSlip slip, List<BitArray> qrModules, Options options, ILogger logger)
        {
            if (!SlipFontResolver.Ensure(options.FontRegular, options.FontBold, logger))
                return null;   // no usable font on this server — the slip page still offers Print

            var brand = ParseColor(options.BrandColorHex);
            var tint = Mix(brand, XColors.White, 0.08);
            var ink = XColor.FromArgb(18, 40, 61);
            var muted = XColor.FromArgb(86, 114, 138);
            var line = XColor.FromArgb(222, 228, 235);

            var doc = new PdfDocument();
            doc.Info.Title = $"Appointment slip {slip.AppointmentId}";
            doc.Info.Author = options.PortalName;

            var page = doc.AddPage();
            page.Width = XUnit.FromPoint(PageW);
            page.Height = XUnit.FromPoint(842);

            using (var g = XGraphics.FromPdfPage(page))
            {
                var tf = new XTextFormatter(g);
                XFont F(double size, bool bold = false) => new(SlipFontResolver.Family, size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);

                // ── Header band ──
                double y = Margin;
                g.DrawRoundedRectangle(new XSolidBrush(brand), Margin, y, ContentW, 64, 10, 10);
                tf.DrawString(Fallback(options.PortalName, slip.HospitalName), F(16, true), XBrushes.White,
                    new XRect(Margin + 20, y + 22, ContentW - 190, 40), XStringFormats.TopLeft);
                g.DrawString("APPOINTMENT SLIP", F(10, true), XBrushes.White,
                    new XRect(Margin, y + 25, ContentW - 20, 14), XStringFormats.TopRight);
                y += 84;

                // ── ID / date / time (left) + QR (right) ──
                const double qrSize = 138;
                var qrX = Margin + ContentW - qrSize;
                g.DrawString("APPOINTMENT ID", F(9, true), new XSolidBrush(muted), Margin, y + 10);
                g.DrawString(slip.AppointmentId, F(28, true), new XSolidBrush(ink), Margin, y + 42);

                // UHID right of the appointment ID ("0" = account created, no appointment booked before)
                if (!string.IsNullOrWhiteSpace(slip.RegistrationNo))
                {
                    var uhidX = Margin + g.MeasureString(slip.AppointmentId, F(28, true)).Width + 26;
                    g.DrawLine(new XPen(line, 1), uhidX - 13, y + 4, uhidX - 13, y + 46);
                    g.DrawString("UHID", F(9, true), new XSolidBrush(muted), uhidX, y + 10);
                    var uhid = slip.RegistrationNo.Trim();
                    var size = uhidX + g.MeasureString(uhid, F(28, true)).Width < qrX - 12 ? 28 : 18;   // long UHID must not reach the QR
                    g.DrawString(uhid, F(size, true), new XSolidBrush(ink), uhidX, y + 42);
                }

                var status = Fallback(slip.Status, "");
                if (status.Length > 0)
                {
                    var w = g.MeasureString(status, F(9, true)).Width + 18;
                    g.DrawRoundedRectangle(new XSolidBrush(tint), Margin, y + 54, w, 18, 9, 9);
                    g.DrawString(status, F(9, true), new XSolidBrush(brand), new XRect(Margin, y + 54, w, 18), XStringFormats.Center);
                }

                DrawTile(g, F, "DATE", Fallback(slip.DateText, "-"), slip.DayText, Margin, y + 84, 150, tint, brand, ink, muted);
                DrawTile(g, F, "TIME", Fallback(slip.TimeText, "-"), "", Margin + 162, y + 84, 150, tint, brand, ink, muted);

                DrawQr(g, qrModules, qrX, y, qrSize);
                g.DrawString("Scan to open this slip", F(8), new XSolidBrush(muted),
                    new XRect(qrX, y + qrSize + 2, qrSize, 12), XStringFormats.TopCenter);
                y += 162;

                // ── Details ──
                g.DrawLine(new XPen(line, 1), Margin, y, Margin + ContentW, y);
                y += 14;
                var rows = new List<(string, string)>
                {
                    ("Patient", slip.PatientName),
                    ("Mobile", slip.Mobile),
                    ("Doctor", slip.DoctorName),
                    ("Speciality", slip.Specialisation),
                    ("Hospital", slip.HospitalName),
                    ("Visit type", slip.VisitType ?? ""),
                    ("Payment", Fallback(slip.PaymentMode, slip.PaymentStatus ?? "")),
                    ("Amount payable", Money(slip.Amount))
                };
                foreach (var (label, value) in rows.Where(r => !string.IsNullOrWhiteSpace(r.Item2)))
                {
                    g.DrawString(label, F(10), new XSolidBrush(muted), Margin, y + 11);
                    var rect = new XRect(Margin + 150, y, ContentW - 150, 30);
                    tf.DrawString(value, F(11, true), new XSolidBrush(ink), rect, XStringFormats.TopLeft);
                    var h = value.Length > 60 ? 30 : 16;   // long values (hospital names) wrap to a second line
                    y += h + 8;
                    g.DrawLine(new XPen(line, 0.6), Margin, y - 4, Margin + ContentW, y - 4);
                }

                // ── Note ──
                y += 12;
                g.DrawRoundedRectangle(new XPen(Mix(brand, XColors.White, 0.3), 0.8), new XSolidBrush(tint),
                    new XRect(Margin, y, ContentW, 58), new XSize(8, 8));
                tf.DrawString(
                    $"Please arrive {options.ArrivalMinutes} minutes before your appointment time and show this slip " +
                    "(printed or on your phone) at the registration counter. The counter can scan the QR code to find your appointment.",
                    F(10), new XSolidBrush(ink), new XRect(Margin + 14, y + 12, ContentW - 28, 40), XStringFormats.TopLeft);

                // ── Footer ──
                g.DrawString($"Generated on {DateTime.Now.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture)}",
                    F(8), new XSolidBrush(muted), Margin, 842 - Margin);
            }

            using var ms = new MemoryStream();
            doc.Save(ms, false);      
            return ms.ToArray();
        }

        private static void DrawTile(XGraphics g, Func<double, bool, XFont> F, string label, string value, string sub,
            double x, double y, double w, XColor tint, XColor brand, XColor ink, XColor muted)
        {
            g.DrawRoundedRectangle(new XSolidBrush(tint), x, y, w, 54, 8, 8);
            g.DrawString(label, F(8, true), new XSolidBrush(brand), x + 12, y + 16);
            g.DrawString(value, F(14, true), new XSolidBrush(ink), x + 12, y + 35);
            if (!string.IsNullOrEmpty(sub)) g.DrawString(sub, F(8, false), new XSolidBrush(muted), x + 12, y + 48);
        }

        // QR modules as vector squares (sharp at any zoom / print size)
        private static void DrawQr(XGraphics g, List<BitArray> modules, double x, double y, double size)
        {
            var n = modules.Count;
            if (n == 0) return;
            var cell = size / n;
            g.DrawRectangle(XBrushes.White, x, y, size, size);
            for (var r = 0; r < n; r++)
                for (var c = 0; c < n; c++)
                    if (modules[r][c])
                        g.DrawRectangle(XBrushes.Black, x + c * cell, y + r * cell, cell + 0.15, cell + 0.15);   // overlap hides hairlines
        }

        // "₹" is missing from some fonts → "Rs."
        private static string Money(string amount) => (amount ?? "").Replace("₹", "Rs.").Trim();

        private static string Fallback(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private static XColor ParseColor(string hex)
        {
            hex = (hex ?? "").TrimStart('#');
            return hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)
                ? XColor.FromArgb((v >> 16) & 255, (v >> 8) & 255, v & 255)
                : XColor.FromArgb(15, 92, 147);
        }

        // amount of `a` mixed into `b` (like CSS color-mix)
        private static XColor Mix(XColor a, XColor b, double amount) => XColor.FromArgb(
            (int)Math.Round(a.R * amount + b.R * (1 - amount)),
            (int)Math.Round(a.G * amount + b.G * (1 - amount)),
            (int)Math.Round(a.B * amount + b.B * (1 - amount)));
    }

    // PDFsharp needs font files. Uses the configured files, else common Windows / Linux fonts.
    internal sealed class SlipFontResolver : IFontResolver
    {
        public const string Family = "SlipSans";
        private static readonly object Gate = new();
        private static bool _ready;
        private static byte[]? _regular, _bold;

        private static readonly string[] RegularCandidates =
        {
            @"C:\Windows\Fonts\arial.ttf", @"C:\Windows\Fonts\segoeui.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/dejavu/DejaVuSans.ttf", "/Library/Fonts/Arial.ttf"
        };
        private static readonly string[] BoldCandidates =
        {
            @"C:\Windows\Fonts\arialbd.ttf", @"C:\Windows\Fonts\segoeuib.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
            "/usr/share/fonts/dejavu/DejaVuSans-Bold.ttf", "/Library/Fonts/Arial Bold.ttf"
        };

        public static bool Ensure(string? regularPath, string? boldPath, ILogger logger)
        {
            if (_ready) return true;
            lock (Gate)
            {
                if (_ready) return true;
                _regular = Load(regularPath, RegularCandidates);
                _bold = Load(boldPath, BoldCandidates) ?? _regular;
                if (_regular == null)
                {
                    logger.LogError("PDF slip: no font file found — set SlipSettings:PdfFontRegular / PdfFontBold to .ttf files");
                    return false;
                }
                if (GlobalFontSettings.FontResolver == null)
                    GlobalFontSettings.FontResolver = new SlipFontResolver();
                _ready = true;
                return true;
            }
        }

        private static byte[]? Load(string? configured, string[] candidates)
        {
            foreach (var path in new[] { configured }.Concat(candidates))
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return File.ReadAllBytes(path);
            return null;
        }

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold ? "SlipSans-Bold" : "SlipSans-Regular");

        public byte[]? GetFont(string faceName) => faceName == "SlipSans-Bold" ? _bold : _regular;
    }
}
