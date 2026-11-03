# Multi-language support (Localization)

The Book Appointment portal can be shown in any language listed in `appsettings.json`.
Out of the box: **English (default)**, **Hindi** and **Bengali**.
The user picks the language from the dropdown in the header; the whole portal — pages, popups,
validation messages, dates, calendar and dropdown search texts — switches to that language.

---

## 1. How it works (request flow)

```
User picks "हिन्दी" in header dropdown
        │  link: /set-language?culture=hi&returnUrl=/PatientDetails/PatientDetails
        ▼
Program.cs  MapGet("/set-language")
        │  • accepts only languages from LanguageSettings
        │  • writes cookie ".AspNetCore.Culture" (1 year)
        │  • redirects back to the same page (local URLs only)
        ▼
Every request: app.UseRequestLocalization()
        │  reads the cookie → sets CurrentCulture / CurrentUICulture = hi
        │  (no cookie → DefaultLanguage = en; browser language is ignored on purpose)
        ▼
Razor pages        @L["Section.Key"]               → JsonStringLocalizer → hi.json
Page models (C#)   _localizer.Text("Section.Key")  → JsonStringLocalizer → hi.json
JavaScript         t('Section.Key')                → window.I18N (written by _Layout from hi.json)
Dates / calendar   .NET culture (server) and Intl API (browser, AppI18n in i18n.js)
```

Wizard data (doctor, slot, OTP status, selected patient) lives in Session, so it is kept when the
language is changed in the middle of a booking.

---

## 2. Files

| File | Purpose |
|---|---|
| `appsettings.json` → `LanguageSettings` | List of languages, default language, translation folder |
| `Models/Settings/LanguageSettings.cs` | Config model (`LanguageSettings`, `LanguageOption`) |
| `Localization/LanguageCatalog.cs` | Validates configured languages, builds cultures (always Gregorian calendar), gives `Current` / `Default` language |
| `Localization/JsonStringLocalizer.cs` | Loads `Resources/Languages/{code}.json` at startup; lookup: current language → default language → key |
| `Resources/Languages/en.json`, `hi.json`, `bn.json` | Translations (one file per language) |
| `Program.cs` | Registers localizer, `RequestLocalizationOptions` (cookie only), `/set-language` endpoint |
| `Pages/_ViewImports.cshtml` | Injects `L` (localizer) and `Languages` (catalog) into every view |
| `Pages/Shared/_Layout.cshtml` | `<html lang>`, script font, `window.I18N` + `window.APP_LANG` for JS, loads `i18n.js` |
| `Pages/Header.cshtml` | Language dropdown (shown only when more than one language is configured) |
| `wwwroot/js/i18n.js` | `t()` helper and `AppI18n` (day/month/AM-PM names, flatpickr locale, select2 texts) |

---

## 3. Configuration

```jsonc
"LanguageSettings": {
  "DefaultLanguage": "en",
  "ResourcesPath": "Resources/Languages",
  "Languages": [
    { "Code": "en", "Name": "English", "NativeName": "English" },
    { "Code": "hi", "Name": "Hindi",   "NativeName": "हिन्दी", "Font": "Noto Sans Devanagari" },
    { "Code": "bn", "Name": "Bengali", "NativeName": "বাংলা",  "Font": "Noto Sans Bengali" }
  ]
}
```

| Field | Meaning |
|---|---|
| `Code` | .NET culture name (`en`, `hi`, `bn`, `ta`, `te`, `mr`, `gu` …). Also the translation file name. |
| `Name` | English name, shown small in the dropdown |
| `NativeName` | Name in its own script, shown in the dropdown and on the header button |
| `Font` | Optional Google Font for non-Latin scripts. When set, the font is loaded and `letter-spacing` is reset (spacing breaks joined scripts). |

Order of `Languages` = order in the dropdown. An unknown or duplicate code is skipped and logged
(`LanguageSettings: unknown or duplicate language code`) — the site keeps running.

---

## 4. Translation files

Nested JSON, flattened to `Section.Key`:

```json
{
  "Review": {
    "Booked": "अपॉइंटमेंट बुक हो गया",
    "AppointmentId": "अपॉइंटमेंट ID"
  }
}
```
→ used as `@L["Review.Booked"]`.

| Section | Used for |
|---|---|
| `Layout`, `Header`, `Footer`, `Steps`, `Common`, `Gender` | Shared UI |
| `Appointment`, `PatientDetails`, `Review` | Page text (Razor) |
| `Validation`, `Errors` | Messages from server **and** JavaScript |
| `Js` | Text built by JavaScript (`Js.Select2.*` = dropdown search texts) |

Rules:
- `{0}`, `{1}` are placeholders — keep them in every language (order may change).
- Keep `OTP`, `UHID`, `SMS` as they are (commonly used in Latin script).
- Only the sections `Js`, `Validation`, `Errors`, `Gender` are sent to the browser (see `_Layout`).
- A missing key falls back to the default language text; if missing there too, the key itself is shown — easy to spot.
- Files are read **at startup** — restart the app after editing a translation file.

### Text coming from appsettings
`HospitalSettings:HeaderText` and `HospitalSettings:DisclaimerText` stay the English source.
For other languages the translation from `Header.Title` / `Footer.DisclaimerText` is used
(`L.ConfigText(...)`). **If you change the English text in appsettings, update these keys in every language file.**

---

## 5. Adding a new language (example: Tamil)

1. **appsettings.json** — add to `LanguageSettings:Languages`:
   ```json
   { "Code": "ta", "Name": "Tamil", "NativeName": "தமிழ்", "Font": "Noto Sans Tamil" }
   ```
2. **Translation file** — copy `Resources/Languages/en.json` to `Resources/Languages/ta.json`
   and translate the **values only** (never the keys, keep `{0}` placeholders).
3. **Restart** the application.

Nothing else is needed: the dropdown, cookie, server culture, JS texts, calendar (month/day names),
slot times (AM/PM) and dropdown search texts pick the language up automatically.

Checklist after adding:
- [ ] Header, all 3 pages, payment popup and booked popup read correctly
- [ ] Long words do not overflow buttons / header title (Tamil words are long — check at phone width too)
- [ ] Validation messages (submit empty form) are translated
- [ ] Log shows `Translations loaded | Language=ta Keys=…` with the same key count as `en`

If `ta.json` is missing, Tamil still appears in the dropdown but shows English text
(log: `Translation file missing for 'ta'`).

---

## 6. Adding new text to the portal

| Where | How |
|---|---|
| Razor view | `@L["Section.Key"]`, with values: `@L["Footer.PanelPatients", phone1, phone2, phone3]` |
| Text with links/markup | `@L.Html("PatientDetails.AgreeTerms", termsLinkHtml, privacyLinkHtml)` — text is HTML-encoded, `{0}` receives the HTML |
| Page model (C#) | `_localizer.Text("Errors.BookingFailed")` — inject `JsonStringLocalizer`. Use `.Text()` (string) in JSON results, not `_localizer[...]` |
| JavaScript | `t('Js.Key')`, `t('Js.PatientsFound', count)` — key must be in `Js`, `Validation`, `Errors` or `Gender` |
| Value from API/config that may have a translation | `@L.TextOr("Gender." + value, value)` |

Then add the key to **every** file in `Resources/Languages`.

---

## 7. Dates, numbers and culture rules

- Display dates follow the selected language (`dd MMM yyyy` → "04 नव॰ 2026").
- **Machine values are always invariant**: session `SlotDate`, API payload (`appFrom_DateTime`, `dob`),
  and parsing of API / `<input type=date>` values use `CultureInfo.InvariantCulture`.
  Never use `DateTime.Parse(x)` / `ToString("yyyy-MM-dd")` without a culture for data that is stored or sent.
- Every culture is forced to the Gregorian calendar (`LanguageCatalog`), so a future language
  with another default calendar (e.g. `ar-SA`, `th-TH`) cannot shift slot or DOB dates.
- Browser side uses Latin digits (`{lang}-u-nu-latn`) to match server-rendered dates.

---

## 8. What is NOT translated

| Item | Why |
|---|---|
| Doctor names, hospital names, specialities, doctor profile (education, treatments) | Come from the hospital API in English. See section 9. |
| `Visit type` / `Status` in the booked popup, API error messages | Returned by the API as text |
| Date input hint `mm/dd/yyyy` | Drawn by the browser, follows the browser/OS language |
| Carousel captions | Text is part of the images |
| Right-to-left layout (Urdu, Arabic) | `dir="rtl"` is set, but the CSS is not mirrored yet |

---

## 9. Translating API data (hospital, speciality, doctor) — options

| Option | Effort | Quality | Notes |
|---|---|---|---|
| **A. API returns localized fields** (e.g. `?lang=hi` or `specialisationName_hi`) | Backend change, small portal change | Best — hospital controls the wording | Recommended for profiles / long text |
| **B. Mapping in translation files** (`Data.Speciality.{id}`, `Data.Facility.{id}`) | Small portal change + keep list updated | Good for short master data | Fine for ~70 specialities and few hospitals; new items show English until added |
| **C. Machine translation service** (Azure/Google Translator + cache) | Medium + running cost | Risky for medical terms | Sends data to a third party; needs review |

Doctor **names** should stay as registered (English): they must match prescriptions, bills and
hospital records — translating or transliterating them causes mismatches.

---

## 10. Troubleshooting

| Symptom | Check |
|---|---|
| Key shown instead of text (e.g. `Review.Booked`) | Key missing in `en.json` |
| English shown for one language | Translation file missing / invalid JSON — see startup log |
| Language not in dropdown | Code invalid (log warning) or only one language configured |
| Language does not change | Cookie blocked; `/set-language` only accepts configured codes |
| JS text in English | Key not in `Js` / `Validation` / `Errors` / `Gender`, or page script runs before `i18n.js` |
| Broken letters in Hindi/Bengali | Missing `Font`, or a CSS rule adding `letter-spacing` |
