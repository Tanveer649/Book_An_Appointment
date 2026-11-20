namespace Book_An_Appointment1.Models.Settings
{
    public class LanguageSettings
    {
        // false = no language dropdown in the header; the whole portal stays in DefaultLanguage
        public bool Enabled { get; set; } = true;
        public string DefaultLanguage { get; set; } = "en";
        public string ResourcesPath { get; set; } = "Resources/Languages";
        public List<LanguageOption> Languages { get; set; } = new();
    }

    public class LanguageOption
    {
        public string Code { get; set; } = "";          // culture name: en, hi, bn, ta-IN ...
        public string Name { get; set; } = "";          // English name (Hindi)
        public string NativeName { get; set; } = "";    // shown in dropdown (हिन्दी)
        public string? Font { get; set; }               // optional Google Font for the script
        public bool IsShow { get; set; } = true;        // false = not offered in the dropdown (and not selectable)
    }
}
