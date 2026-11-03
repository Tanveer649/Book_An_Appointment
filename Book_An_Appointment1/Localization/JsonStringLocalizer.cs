using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Book_An_Appointment1.Localization
{
    // Translations from {ResourcesPath}/{code}.json — nested sections are flattened to "Section.Key".
    // Lookup order: current language → default language → the key itself (so a missing key is visible, never a crash).
    public class JsonStringLocalizer : IStringLocalizer
    {
        private readonly LanguageCatalog _catalog;
        private readonly Dictionary<string, Dictionary<string, string>> _texts = new(StringComparer.OrdinalIgnoreCase);

        public JsonStringLocalizer(LanguageCatalog catalog, IWebHostEnvironment env, ILogger<JsonStringLocalizer> logger)
        {
            _catalog = catalog;
            var folder = Path.Combine(env.ContentRootPath, catalog.Settings.ResourcesPath);

            foreach (var lang in catalog.Languages)
            {
                var file = Path.Combine(folder, lang.Code + ".json");
                if (!File.Exists(file))
                {
                    logger.LogWarning("Translation file missing for '{Code}' ({File}) — default language text will be shown", lang.Code, file);
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(file),
                        new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    Flatten(doc.RootElement, "", dict);
                    _texts[lang.Code] = dict;
                    logger.LogInformation("Translations loaded | Language={Code} Keys={Count}", lang.Code, dict.Count);
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Invalid translation file {File} — default language text will be shown", file);
                }
            }
        }

        // ── IStringLocalizer ─────────────────────────────────
        public LocalizedString this[string name]
        {
            get
            {
                var value = Find(name);
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var value = Find(name);
                return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value ?? name, arguments), value == null);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var current = Dictionary(_catalog.Current.Code) ?? new();
            var keys = includeParentCultures
                ? current.Keys.Union(Dictionary(_catalog.Default.Code)?.Keys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase)
                : current.Keys;
            return keys.Select(k => this[k]);
        }

        // ── Helpers for pages / page models ─────────────────
        // Plain string (use in C# — anonymous JSON objects would otherwise serialize the LocalizedString object)
        public string Text(string key, params object[] args) => args.Length == 0 ? this[key].Value : this[key, args].Value;

        // Translation when it exists, otherwise the given value — for API / config values like "Male"
        public string TextOr(string key, string? fallback)
        {
            var value = Find(key);
            return value ?? fallback ?? "";
        }

        // Text configured in appsettings (English): used as-is for the default language,
        // translated via the key for other languages when a translation exists.
        public string ConfigText(string key, string? configValue)
        {
            if (!string.IsNullOrWhiteSpace(configValue) && IsDefaultLanguage)
                return configValue;

            var translated = Dictionary(_catalog.Current.Code)?.GetValueOrDefault(key);
            return translated ?? configValue ?? Text(key);
        }

        // Translation containing markup slots: the text is HTML-encoded, {0}, {1} ... receive the given HTML
        public IHtmlContent Html(string key, params string[] htmlArgs)
        {
            var encoded = HtmlEncoder.Default.Encode(Find(key) ?? key);
            return new HtmlString(string.Format(CultureInfo.CurrentCulture, encoded, htmlArgs));
        }

        // Keys of the given sections for JavaScript (window.I18N), with default-language fallback
        public Dictionary<string, string> ForScript(params string[] sections)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var code in new[] { _catalog.Default.Code, _catalog.Current.Code }.Distinct())
            {
                var dict = Dictionary(code);
                if (dict == null) continue;
                foreach (var pair in dict.Where(p => sections.Any(s => p.Key.StartsWith(s + ".", StringComparison.OrdinalIgnoreCase))))
                    result[pair.Key] = pair.Value;
            }
            return result;
        }

        public bool IsDefaultLanguage => string.Equals(_catalog.Current.Code, _catalog.Default.Code, StringComparison.OrdinalIgnoreCase);

        // ── Private ──────────────────────────────────────────
        private string? Find(string key) =>
            Dictionary(_catalog.Current.Code)?.GetValueOrDefault(key)
            ?? Dictionary(_catalog.Default.Code)?.GetValueOrDefault(key);

        private Dictionary<string, string>? Dictionary(string code) => _texts.GetValueOrDefault(code);

        private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> dict)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                    Flatten(prop.Value, string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}", dict);
            }
            else if (element.ValueKind == JsonValueKind.String)
            {
                dict[prefix] = element.GetString() ?? "";
            }
        }
    }
}
