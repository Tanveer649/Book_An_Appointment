using Book_An_Appointment1.Models.Settings;
using System.Globalization;

namespace Book_An_Appointment1.Localization
{
    // Languages from appsettings LanguageSettings that .NET recognises — drives the header dropdown,
    // the request culture and the translation files that get loaded.
    public class LanguageCatalog
    {
        public LanguageSettings Settings { get; }
        public IReadOnlyList<LanguageOption> Languages { get; }
        public IReadOnlyList<CultureInfo> Cultures { get; }
        public LanguageOption Default { get; }

        // Codes skipped because .NET does not know the culture (logged at startup)
        public IReadOnlyList<string> InvalidCodes { get; }

        public LanguageCatalog(LanguageSettings settings)
        {
            Settings = settings;
            var languages = new List<LanguageOption>();
            var cultures = new List<CultureInfo>();
            var invalid = new List<string>();

            foreach (var lang in settings.Languages.Where(x => !string.IsNullOrWhiteSpace(x.Code)))
            {
                var culture = CreateCulture(lang.Code.Trim());
                if (culture == null || cultures.Any(c => c.Name == culture.Name))
                {
                    invalid.Add(lang.Code);
                    continue;
                }
                lang.Code = culture.Name;
                if (string.IsNullOrWhiteSpace(lang.NativeName)) lang.NativeName = culture.NativeName;
                if (string.IsNullOrWhiteSpace(lang.Name)) lang.Name = culture.EnglishName;
                languages.Add(lang);
                cultures.Add(culture);
            }

            // Always have at least the default language
            if (languages.Count == 0)
            {
                var fallback = CreateCulture(settings.DefaultLanguage) ?? CreateCulture("en")!;
                languages.Add(new LanguageOption { Code = fallback.Name, Name = fallback.EnglishName, NativeName = fallback.NativeName });
                cultures.Add(fallback);
            }

            Languages = languages;
            Cultures = cultures;
            InvalidCodes = invalid;
            Default = Find(settings.DefaultLanguage) ?? languages[0];
        }

        // Language of the current request (set by RequestLocalization from the cookie)
        public LanguageOption Current => Find(CultureInfo.CurrentUICulture.Name) ?? Default;

        public LanguageOption? Find(string? code) =>
            string.IsNullOrWhiteSpace(code)
                ? null
                : Languages.FirstOrDefault(x => string.Equals(x.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

        public CultureInfo CultureOf(LanguageOption lang) =>
            Cultures.First(c => string.Equals(c.Name, lang.Code, StringComparison.OrdinalIgnoreCase));

        // Gregorian calendar even for cultures whose default calendar differs (e.g. ar-SA, th-TH),
        // so slot / DOB dates never shift when a new language is added in config.
        private static CultureInfo? CreateCulture(string code)
        {
            try
            {
                var culture = new CultureInfo(code);
                if (culture.Calendar is not GregorianCalendar)
                {
                    var gregorian = culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
                    if (gregorian != null) culture.DateTimeFormat.Calendar = gregorian;
                }
                return culture;
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }
    }
}
