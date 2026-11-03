namespace Book_An_Appointment1.Models.Settings
{
    public class LanguageSettings
    {
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
    }
}
