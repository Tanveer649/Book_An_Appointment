namespace Book_An_Appointment1.Helpers
{
    // "Male" / "M" / "1" → hospital API gender code from appsettings AppointmentApi:GenderCodes
    public static class GenderCode
    {
        public static string Resolve(IConfiguration configuration, string? gender, ILogger? logger = null)
        {
            var g = gender?.Trim();
            if (string.IsNullOrEmpty(g)) return "";
            if (int.TryParse(g, out _)) return g;   // already a code

            var match = configuration.GetSection("AppointmentApi:GenderCodes").GetChildren()
                .FirstOrDefault(x => string.Equals(x.Key, g, StringComparison.OrdinalIgnoreCase) ||
                                     (g.Length == 1 && x.Key.StartsWith(g, StringComparison.OrdinalIgnoreCase)));

            if (match == null)
                logger?.LogWarning("Gender code not configured | Gender={G}", g);

            return match?.Value ?? "";
        }
    }
}
