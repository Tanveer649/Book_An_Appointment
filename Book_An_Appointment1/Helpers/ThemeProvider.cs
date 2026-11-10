using System.Text.RegularExpressions;

namespace Book_An_Appointment1.Helpers
{
    // Hospital theme: appsettings.json → ThemeSettings:Theme = "aiims" loads wwwroot/themes/aiims.css.
    // Unknown / unsafe name or missing file → themes/default.css (warning logged once per name).
    // Read on every page, so editing appsettings.json switches the theme without a rebuild.
    public class ThemeProvider
    {
        private const string Folder = "themes";
        private const string DefaultTheme = "default";
        private static readonly Regex SafeName = new("^[a-z0-9-]{1,50}$", RegexOptions.Compiled);

        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ThemeProvider> _logger;
        private string? _lastWarned;

        public ThemeProvider(IConfiguration configuration, IWebHostEnvironment environment, ILogger<ThemeProvider> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        // "~/themes/aiims.css", or null when not even default.css is deployed (theme.css defaults apply)
        public string? ThemeUrl
        {
            get
            {
                var name = (_configuration["ThemeSettings:Theme"] ?? "").Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(name)) name = DefaultTheme;

                if (!SafeName.IsMatch(name) || !Exists(name))
                {
                    WarnOnce(name);
                    name = DefaultTheme;
                    if (!Exists(name)) return null;
                }
                return $"~/{Folder}/{name}.css";
            }
        }

        // Hospital brand colour (--primary of the theme file) for places CSS cannot reach, e.g. the PDF slip
        public string PrimaryColor
        {
            get
            {
                var url = ThemeUrl;
                if (url == null) return FallbackPrimary;
                var file = _environment.WebRootFileProvider.GetFileInfo(url.TrimStart('~', '/'));
                if (!file.Exists) return FallbackPrimary;

                using var reader = new StreamReader(file.CreateReadStream());
                var match = PrimaryVar.Match(reader.ReadToEnd());
                return match.Success ? match.Groups[1].Value : FallbackPrimary;
            }
        }

        private const string FallbackPrimary = "#0F5C93";   // theme.css default
        private static readonly Regex PrimaryVar = new(@"--primary\s*:\s*(#[0-9a-fA-F]{6})\b", RegexOptions.Compiled);

        private bool Exists(string name) =>
            _environment.WebRootFileProvider.GetFileInfo($"{Folder}/{name}.css").Exists;

        private void WarnOnce(string name)
        {
            if (_lastWarned == name) return;
            _lastWarned = name;
            _logger.LogWarning("Theme '{Theme}' not found (wwwroot/{Folder}/{Theme}.css) or invalid name — using '{Default}'",
                name, Folder, name, DefaultTheme);
        }
    }
}
