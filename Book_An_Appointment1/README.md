# Book An Appointment — Hospital Patient Portal

Online doctor-appointment booking for hospitals. One codebase is deployed per hospital (client);
each deployment differs only in `appsettings.json` (API address + credentials, theme, languages,
feature switches). All hospital data (facilities, specialities, doctors, slots, patients, bookings)
comes live from the hospital's HIS API.

> **Keep this file up to date** — every new feature, setting or public URL is added here.
> Feature details live in [`Docs`](Docs).

---

## Contents

1. [Features](#1-features)
2. [Tech stack](#2-tech-stack)
3. [Run locally](#3-run-locally)
4. [Project structure](#4-project-structure)
5. [Public URLs](#5-public-urls)
6. [Configuration (appsettings.json)](#6-configuration-appsettingsjson)
7. [Deploying for a new hospital](#7-deploying-for-a-new-hospital)
8. [App_Data/keys — what it is](#8-app_datakeys--what-it-is)
9. [Hospital API endpoints used](#9-hospital-api-endpoints-used)
10. [Libraries & licences](#10-libraries--licences)
11. [Documentation index](#11-documentation-index)
12. [Changelog](#12-changelog)

---

## 1. Features

| Area | What it does | Doc |
|---|---|---|
| **Booking wizard** | 3 steps: hospital → speciality / doctor → date/slot · patient details · review & confirm. The default facility (`ApiParam:GetFacilitiesParams:FacilityCode`) is pre-selected; the patient can pick a **speciality first or a doctor first** (choosing a doctor fills in its speciality). Choices survive a page reload and Back from page 2; a **Start over** link (with confirmation) clears them when the patient wants (`AppointmentPageSettings:EnableStartOver`). | — |
| **New patient** | Details form + mobile OTP verification. | — |
| **Registered patient** | Mobile OTP → family members on that number as cards → pick one. UHID `0` = account created, no appointment booked before. | — |
| **Add family member** | Registered patient can add a member (MobSaveRegistrationForPatient); it is selected straight away. | [Family-Member.md](Docs/Family-Member.md) |
| **Payment** | *Pay on arrival* (booked popup) or *Pay now* (redirect to the payment gateway with `appointmentId`). | — |
| **Appointment slip** | After Pay on arrival: QR code, PDF download, optional Print / View slip / Add to calendar. | [Appointment-Slip.md](Docs/Appointment-Slip.md) |
| **Doctor profile + website link** | Public profile `/doctor/{id}`; hospital website links to `/book/doctor/{id}` → doctor (and optionally day/slot) pre-selected. | [Doctor-Link.md](Docs/Doctor-Link.md) |
| **Next free day** | A day without free slots (e.g. late evening) shows "No free slots on … · Next free day is …" with a **Go to …** button (`AppointmentPageSettings:EnableNextAvailable`). | — |
| **Find my slip** | `/find-slip`: mobile + OTP → the patient's appointment slips (PDF), linked from the first page (`FindSlipSettings`). | [Appointment-Slip.md](Docs/Appointment-Slip.md#find-my-slip) |
| **Hide list** | Facilities / doctors / specialities the client does not want online — hidden in dropdowns, links and profiles (`HideList`). | [Deploy-New-Client.md](Docs/Deploy-New-Client.md#211-hide-doctors--specialities) |
| **Booking QR poster** | `/booking-qr` — printable wall poster with a QR code that opens the portal (+ PNG / SVG). | [section 5](#5-public-urls) |
| **Languages** | English / Hindi / Bengali (more by adding a JSON file + config). | [Language-Feature.md](Docs/Language-Feature.md) |
| **Themes** | Hospital colours from one CSS file per hospital; 20 ready themes. | [Theme.md](Docs/Theme.md) |
| **Security** | One-time step URLs, server-side checks, rate limits, CSP / security headers, masked logs. | [Security.md](Docs/Security.md) |

---

## 2. Tech stack

- **ASP.NET Core Razor Pages, .NET 10** (C#), session-based booking wizard
- Front end: Bootstrap 5, jQuery, Select2, Flatpickr (CDN, version-pinned with SRI hashes), plain JS per page
- Hospital API via `HttpClient` (+ bearer token cached in memory), Newtonsoft.Json
- QRCoder (QR codes), PDFsharp 6.2.4 (slip PDF)
- ASP.NET Data Protection (antiforgery, session cookie, signed slip links), built-in rate limiter

---

## 3. Run locally

```bash
dotnet run --project "Book _An_Appointment.csproj" --launch-profile http
```

(run from this project folder)

Visual Studio: open `Book-An-Appointment1.slnx` and press F5 (IIS Express → https://localhost:44393).

**Development environment** (`ASPNETCORE_ENVIRONMENT=Development`):
- Send OTP does **not** send an SMS; the test OTP **`123456`** is accepted.
- Registered-patient OTP `123456` returns a test family; *Add family member* is simulated (nothing is created in the HIS).
- Booking (CreateAppointment) still calls the real API configured in `ApiSettings`.

Staging / Production always use the real OTP APIs.

### Local URLs (development)

| How you run it | Base address |
|---|---|
| Visual Studio — IIS Express (F5) | **https://localhost:44393** |
| `dotnet run --launch-profile http` | http://localhost:5105 |
| `dotnet run --launch-profile https` | https://localhost:7030 |

Useful pages (IIS Express):

| Page | URL |
|---|---|
| Start a booking | https://localhost:44393/book |
| Doctor profile | https://localhost:44393/doctor/18908?facilityId=1 |
| Website link — doctor pre-selected | https://localhost:44393/book/doctor/18908?facilityId=1 |
| Website link — doctor + day + time | https://localhost:44393/book/doctor/18908?facilityId=1&date=2026-11-06&slot=09:30 |
| Booking QR wall poster | **https://localhost:44393/booking-qr** |
| Poster QR as PNG / SVG | https://localhost:44393/booking-qr?handler=Png · https://localhost:44393/booking-qr?handler=Svg |

On a server replace `https://localhost:44393` with the portal address, e.g.
**https://<portal>/book/doctor/18908?facilityId=1** and **https://<portal>/booking-qr**.

---

## 4. Project structure

```
Book_An_Appointment1/   (this folder)
├─ README.md            This file
├─ API/                 HTTP clients, routes (ApiRoutes.cs), auth handler (bearer token)
├─ App_Data/keys/       Data Protection keys — created at runtime, NOT in git (see section 8)
├─ logs/               Daily log files (FileLogging) — created at runtime, last 5 days kept, NOT in git
├─ App_Data/slips/      "Find my slip" records — created at runtime, NOT in git
├─ Docs/                Feature documentation
├─ Helpers/             Session, theme, slip PDF, gender codes, log masking
├─ Localization/        JSON string localizer + language catalog
├─ Models/              API request / response models
├─ Pages/
│  ├─ Appointment/      Step 1 — hospital / speciality / doctor / slot
│  ├─ PatientDetails/   Step 2 — new / registered patient, family members
│  ├─ ReviewAndConfirmation/  Step 3 — review, payment, booked popup
│  ├─ Slip/             /slip/{link} — appointment slip page, PDF, calendar
│  ├─ WebsiteDoctorLink/  Pages the hospital website links to:
│  │                    DoctorBookingLink → /book/doctor/{id} (doctor pre-selected), DoctorProfile → /doctor/{id}
│  ├─ BookingQr/        /booking-qr — wall poster
│  └─ FindSlip/         /find-slip — Find my slip (mobile + OTP → slips)
├─ Resources/Languages/ en.json, hi.json, bn.json
├─ Security/            Booking-flow URLs, rate limiting, security headers
├─ Services/            Business services (doctor directory, slip, OTP, booking, …)
├─ ViewModule/          Page view models + session data
├─ wwwroot/             css (theme.css + page css), js/pages, themes/{hospital}.css, images
├─ appsettings.json     All per-hospital settings
└─ Program.cs           Service registration + middleware pipeline
```

---

## 5. Public URLs

| URL | What |
|---|---|
| `/` , `/book` | Start / continue a booking (redirects to the current step) |
| `/appointment/{id}`, `/patientdetails/{id}`, `/reviewconfirm/{id}` | Booking steps. `{id}` is a 64-char one-time value; only buttons move between steps. |
| `/doctor/{doctorId}?facilityId=1` | Public doctor profile (shareable) |
| `/book/doctor/{doctorId}?facilityId=1[&date=yyyy-MM-dd][&slot=HH:mm]` | **Hospital website link** — opens booking with the doctor (and day / slot) pre-selected. Example: `https://<portal>/book/doctor/18908?facilityId=1` |
| `/slip/{link}` (`?handler=Pdf`, `?handler=Ics`) | Appointment slip (signed link, from the booked popup / QR) |
| `/find-slip` | Find my slip — mobile + OTP → appointment slips (`FindSlipSettings:Enabled`) |
| `/booking-qr` (`?handler=Png`, `?handler=Svg`) | Wall poster with the booking QR code (`BookingQrSettings:Enabled`). Example: `https://<portal>/booking-qr` |
| `/set-language?culture=hi&returnUrl=/` | Change language |

---

## 6. Configuration (appsettings.json)

| Section | Key settings | Notes |
|---|---|---|
| `ApiSettings` | `BaseUrl`, `UserName`, `Password`, `TimeoutSeconds` | **Per hospital.** HIS API address + portal account. |
| `HospitalSettings` | `HospitalName`, `LogoPath`, `LogoHeight` (shown 32–60 px, recommended `56px`), `LogoWidth` (`auto`, max 200 px), `HeaderText`, `EmergencyNumber`, `DisclaimerText`, footer links (`AboutUsUrl`, `ContactUs`, `TermsUrl`, `RefundPolicyUrl`, `PrivacyPolicyUrl` — empty = hidden) | Header, footer, slip and poster branding. Logo file: PNG/SVG, ≥ 112 px high. |
| `FooterSettings` | `Enabled`, `ShowDisclaimer`, `DisclaimerCollapsed`, `ShowPanelPhones`, `ShowAboutUs`, `ShowContactUs`, `ShowTerms`, `ShowRefundPolicy`, `ShowPrivacyPolicy`, `ShowGooglePlay` + `GooglePlayUrl`, `ShowAppStore` + `AppStoreUrl`, `ShowCopyright` + `CopyrightYear` / `CopyrightName`, `ShowSecureBadge` | Compact footer. Every part has its own `Show…` switch; `Enabled: false` removes the whole footer. See [Footer.md](Docs/Footer.md) |
| `ThemeSettings` | `Theme` | Loads `wwwroot/themes/{Theme}.css` |
| `LanguageSettings` | `DefaultLanguage`, `Languages[]` | Languages in the header dropdown |
| `PaymentSettings` | `EnablePayNow`, `EnablePayOnArrival`, `PaymentGatewayUrl` | Payment options |
| `AppointmentApi` | `Source`, `AppointmentType`, `SourceAppointmentId`, `DefaultSlotMinutes`, `GenderCodes` | Values sent to CreateAppointment |
| `OtpSettings` | `OtpLength`, `NotRegisteredPattern`, `AlreadyRegisteredPattern` | OTP length + how the API reports "wrong patient type" |
| `RateLimits` | `WindowMinutes`, `OtpSend`, `OtpVerify`, `Booking` | Requests per visitor (IP) per window |
| `AppointmentPageSettings` | `EnableStartOver`, `EnableNextAvailable` | "Start over" link; "Next free day" box on days without slots (`true` = on) |
| `HideList` | `Enabled`, `FacilityIds`, `DoctorIds`, `SpecialityIds` | Facilities / doctors / specialities hidden from patients (ids as strings) |
| `FindSlipSettings` | `Enabled`, `StorePath` | Find my slip page; slips remembered in `App_Data/slips` (or `StorePath`) |
| `MaxAppointmentAllowedDays` | `Days` | Booking window (default 90) |
| `SlipSettings` | `LinkValidDays`, `ArriveMinutesBefore`, `ReminderMinutesBefore`, `TimeZone`, `PublicBaseUrl`, **`EnablePrint`**, **`EnableViewSlip`**, **`EnableAddToCalendar`** | Appointment slip. The three `Enable…` switches show / hide those buttons (all `false` now). |
| `BookingQrSettings` | `Enabled`, `Url`, `Title`, `Subtitle` | Wall poster `/booking-qr`. `Url` empty = this site + `/book`. |
| `FamilyMemberSettings` | `DefaultAreaId`, `PaymentFlag`, `OtpErrorPattern` | Add family member (MobSaveRegistrationForPatient) |
| `FileLogging` | `Enabled`, `Path`, `RetainDays` | Daily log files `logs/portal-yyyyMMdd.log`; only the last `RetainDays` days are kept (default 5) |
| `DataProtection` | `KeysPath` | Where the signing keys are stored (section 8) |
| `ApiParam` | per-API static query values | `GetFacilitiesParams:FacilityCode` = **default facility** selected on the first page (matched to the facility id); location id, default country, … |

---

## 7. Deploying for a new hospital

Full step-by-step guide (what to change in appsettings.json, logo and photo sizes, keys, IIS, checklist):
**[Docs/Deploy-New-Client.md](Docs/Deploy-New-Client.md)**. Short version:

1. Publish the project (same build for every hospital).
2. In `appsettings.json` set:
   - `ApiSettings` (BaseUrl, UserName, Password)
   - `HospitalSettings` (name, logo, emergency number, footer)
   - `ThemeSettings:Theme` + make sure `wwwroot/themes/{theme}.css` exists
   - `ApiParam` facility / location values
   - `PaymentSettings:PaymentGatewayUrl`
   - usually **leave** `SlipSettings:PublicBaseUrl` empty. It is **this portal's** public address (never the API URL); set it only behind a proxy when QR codes show an internal address
3. IIS: nothing to set for the environment — a published build runs as **Production** automatically. Recommended: give the app-pool user write access to `App_Data` (the keys folder is created automatically; without write access the portal still runs, see section 8).
4. Never delete `App_Data/keys` on redeploy (section 8).
5. Print the wall poster from `https://<portal>/booking-qr`.
6. Send [Doctor-Link.md](Docs/Doctor-Link.md) to the hospital website team.
7. Go through the checklist in [Security.md](Docs/Security.md#10-before-go-live-checklist).

---

## 8. App_Data/keys — what it is

`App_Data/keys/key-….xml` files are **ASP.NET Data Protection keys** — *not* API keys and not passwords
for the hospital API. ASP.NET Core uses them to sign / encrypt:

- **appointment slip links and their QR codes** (`/slip/{link}`) — so nobody can change the appointment ID in a link,
- **antiforgery tokens** (every form and AJAX post),
- the **session cookie**.

The folder is created automatically on first start — **nothing to set up**. If the site cannot write there, the portal still runs with ASP.NET's default key storage and logs a warning. Rules:

| Do | Why |
|---|---|
| Keep it between deployments | If it is deleted, all slip links / QR codes already given to patients stop working and open sessions end. |
| Give the IIS app-pool user write access | ASP.NET adds a new key every 90 days. |
| Production: set `DataProtection:KeysPath` to a folder **outside** the site folder (e.g. `D:PortalKeys<client>`) | A publish that deletes old files can then never wipe the keys. |
| One set of keys **per client** — never copy keys between clients, never put the key itself in appsettings | Each deployment creates its own keys automatically; appsettings only holds the folder path. |
| Several servers → set `DataProtection:KeysPath` to **one shared folder** | Otherwise a link made on server A fails on server B. |
| Never commit it to git | It is in `.gitignore`. On Windows the files are additionally encrypted for the machine (DPAPI). |

The hospital **API credentials** are in `appsettings.json → ApiSettings` (UserName / Password).

---

## 9. Hospital API endpoints used

All under `ApiSettings:BaseUrl` (see `API/EndPoints/ApiRoutes.cs`):

| Endpoint | Used for |
|---|---|
| `/api/Token/GetToken` | Bearer token (cached) |
| `/api/VCP/GetFacilityDetails` · `GetSpecialityList` · `GetDoctorList` | Hospitals, specialities, doctors (doctor by id for links / profiles) |
| `/api/VCP/GetDoctorSlots` · `GetConsultationCharges` | Slots, fees |
| `/api/VCP/GenerateOTPForSignup` · `GenerateOTPForPatient` · `VerifyPatientOTP` | OTP (new / registered patient) |
| `/api/vcp/GetCountryList` · `GetStateList` · `GetCityList` · `GetTitles` | Form lists |
| `/api/VCP/MobSaveRegistrationForPatient` | Add family member |
| `/api/VCP/CreateAppointment` · `GetAppointmentByID` | Booking, booked popup, slip |

---

## 10. Libraries & licences

| Package | Version | Licence |
|---|---|---|
| Newtonsoft.Json | 13.0.4 | MIT |
| QRCoder | 1.8.1 | MIT |
| PDFsharp | **6.2.4 (pinned)** | MIT |
| Bootstrap / jQuery / Select2 / Flatpickr (CDN) | pinned | MIT |

> ⚠ **Do not upgrade PDFsharp to 6.5+** — it changed to **AGPL-3.0**, which would require publishing this
> portal's source code. The version is pinned as `[6.2.4]` in the `.csproj`.

---

## 11. Documentation index

| Document | For |
|---|---|
| [Deployment.md](Docs/Deployment.md) | **Publish, first install and updates (redeploy)** on IIS — what the publish contains, the 3 ways to update, App_Data rules, rollback |
| [Deploy-New-Client.md](Docs/Deploy-New-Client.md) | **Deploying for a new hospital** — every appsettings change, logo / photo sizes, keys, IIS, go-live checklist |
| [Doctor-Link.md](Docs/Doctor-Link.md) | **Hospital website team** — link format for doctor profiles / "Book appointment" |
| [Appointment-Slip.md](Docs/Appointment-Slip.md) | Slip, QR, PDF, print, calendar, deployment of keys |
| [Family-Member.md](Docs/Family-Member.md) | Add family member flow + API mapping |
| [Language-Feature.md](Docs/Language-Feature.md) | Languages, adding a new one |
| [Theme.md](Docs/Theme.md) | Hospital colours / themes |
| [Footer.md](Docs/Footer.md) | Footer links, disclaimer, copyright, Google Play / App Store badges |
| [Security.md](Docs/Security.md) | Security design + go-live checklist |

---

## 12. Changelog

Add a line for every change that affects users, settings or URLs (newest first).

| Date | Change |
|---|---|
| 2026-11-12 | Phone (≤ 767 px) tidy-up, desktop unchanged. Patient Details: the 6 OTP boxes always stay on one line; registered-patient cards show only avatar, name, UHID and the tick — an arrow opens / closes gender, date of birth and email (tapping the card still selects). Review & Confirm: subtitle and the second info line removed (one slim "check all details" line), emoji icons dropped; appointment details two per row (Hospital and Doctor full width, Date / Time, Specialty / Fee); patient information two per row with the name first; tighter cards and payment summary. New key `Js.ShowDetails` (en/hi/bn). |
| 2026-11-12 | Phone / tablet layout (desktop ≥ 992 px unchanged). ≤ 991 px: no hospital photo carousel (images not even downloaded); one-tap date strip replaces the big calendar (month calendar opens from a "Calendar" button and closes after a pick); "Your slot" moves into a floating Continue bar that parks at the end of the booking panel; Review & Confirm bar is now sticky instead of fixed, so it stops above the footer. ≤ 767 px: welcome step list hidden, compact doctor card with details folded behind "View doctor details", slots in Morning / Afternoon / Evening tabs (3 per row, no inner scroll). Footer slightly tighter on phones. New keys `Js.DoctorDetails`, `Js.Calendar`, `Js.PickTime` (en/hi/bn). |
| 2026-11-11 | Booked slots: GetDoctorSlots `statusText` is now read. "Available" = open; "Booked" (or any other status) = disabled button with grey diagonal stripes and faded time (tooltip "Booked"). On days with a booked slot, an "Available / Booked" key is pinned above the slot list. The Morning/Afternoon counts show only free slots. The server also refuses a booked slot on Continue, a remembered booked slot is not restored, and a day where every slot is booked counts as having no free slots (shows "Next free day"). New keys `Js.SlotBooked`, `Js.SlotAvailable` (en/hi/bn). |
| 2026-11-11 | Slip page (`/slip/…`, opened from the QR code) tidied for phones. Appointment ID and UHID sit in two equal boxes, Date and Time side by side, and short details (mobile, visit type, payment, amount) two per row. The empty Specialty row is hidden, there is a proper info icon, and spacing is tighter. |
| 2026-11-11 | Footer on/off switches in `FooterSettings` for every part: `Enabled` (whole footer), `ShowDisclaimer`, `ShowAboutUs`, `ShowContactUs`, `ShowTerms`, `ShowRefundPolicy`, `ShowPrivacyPolicy`, `ShowGooglePlay`, `ShowAppStore`, `ShowCopyright`, `ShowSecureBadge`. |
| 2026-11-11 | Disclaimer rewritten (en / hi / bn): open to all patients, pay online or at the hospital, arrive 15 minutes early with photo ID, timings may change, contact the hospital for changes / cancellations. |
| 2026-11-11 | **Title decides gender**, following the HIS title master (`GetTitles` → `Gender`). **M** (Mr., Master, Unit) allows Male only, **F** (Mrs., Ms., Baby) allows Female only, and **B** (Dr., Baby Of) allows any gender. The other buttons are greyed out on the New patient and Add family member forms, and the server rejects a mismatch ("This gender does not match the selected title"). |
| 2026-11-11 | **Gender codes swapped for this HIS**: `AppointmentApi:GenderCodes` is now Male `2`, Female `1`. This was confirmed in the HIS, where a Female member was saved as Male. It applies to CreateAppointment and Add family member. Patients created earlier may have the wrong gender in the HIS. |
| 2026-11-11 | Shorter, clearer disclaimer text (`HospitalSettings:DisclaimerText` + `Footer.DisclaimerText` in en / hi / bn). `FooterSettings` re-added to appsettings (it had been lost, so the copyright line and store badge were missing). Add family member: the new card shows the gender chosen in the form. The hospital API sent the gender back swapped (Ms. + Female → "Male"), and that mismatch is now logged as a warning. |
| 2026-11-11 | Compact footer, configured in appsettings (`FooterSettings` + links in `HospitalSettings`). It has a one-line disclaimer with Read more, a Contact us link, the copyright line, and Google Play / App Store badges. Empty values hide their part, and external links open in a new tab. Removed a linked `akhil-logo.png` from the Downloads folder in the `.csproj`, which broke the build (the file is in `wwwroot/Images/Logo`). See [Footer.md](Docs/Footer.md). |
| 2026-11-10 | Fix — Add family member: `IsFamilyMember` and `inyHospitalLocationId` are now sent as **numbers** (`1`), not strings (`"1"`). The API could not read the string `"1"` into its bool, so the procedure got `@bitIsFamilyMember=0` and the member was not added. Request body is logged at Debug (OTP masked). |
| 2026-11-10 | Hide list can also hide **facilities** (`HideList:FacilityIds`) — not in the facility dropdown, their specialities / doctors not loaded, links and profiles say not found, Continue refuses them. |
| 2026-11-10 | Log files: `logs/portal-yyyyMMdd.log` one per day, only the last `FileLogging:RetainDays` (5) days kept, older files deleted automatically. |
| 2026-11-08 | Find my slip shows **patient name and UHID** on each slip (family members on one mobile). The patient name is also kept in the slip record and the signed slip link, because GetAppointmentByID often returns no name. |
| 2026-11-08 | Fees shown in whole rupees on every page (1.50 → 2, rounded the same way everywhere — `Helpers/Fee.cs`); the Review total is the sum of the rounded lines. |
| 2026-11-08 | Next free day box (`EnableNextAvailable`), Find my slip page `/find-slip` (`FindSlipSettings`), doctor / speciality hide list (`HideList`). |
| 2026-11-08 | New [Deployment.md](Docs/Deployment.md): publish contents, first install, 3 ways to redeploy (App_Data is not in the publish; deleting it only invalidates old slip links), rollback. |
| 2026-11-07 | First page: "Start over" link (top-right of the dropdown bar, with confirmation) clears speciality / doctor / slot — reload and Back still keep the choices. Switch: `AppointmentPageSettings:EnableStartOver`. |
| 2026-11-07 | First page: default facility pre-selected from `GetFacilitiesParams:FacilityCode`; specialities and all doctors of the facility load on page open; doctor can be chosen first (its speciality is filled in and the doctor list narrows to it). GetDoctors no longer sends doctor mobile / e-mail to the browser. |
| 2026-11-06 | Keys folder falls back safely when not writable (warning in log). Deploy guide: short answers on environment, PublicBaseUrl (portal address, not API URL) and keys. |
| 2026-11-06 | `Pages/Doctor` renamed to `Pages/WebsiteDoctorLink` (`DoctorBookingLink`, `DoctorProfile`) — URLs unchanged. Header logo kept inside a safe size box. New [Deploy-New-Client.md](Docs/Deploy-New-Client.md). |
| 2026-11-05 | README moved into the project folder (visible in Visual Studio); local / example URLs added. |
| 2026-11-05 | Booking QR wall poster (`/booking-qr`, `BookingQrSettings`). Doctor link accepts optional `date` / `slot`. `SlipSettings:EnablePrint`. README added. |
| 2026-11-05 | Slip shows UHID next to appointment ID; View slip / Add to calendar switchable (`SlipSettings`). Add family member uses the verified OTP (no second OTP). |
| 2026-11-05 | Appointment slip (QR, PDF, print, calendar), doctor profile + website deep link, Add family member. |
| 2026-11-04 | Slot, notes and consent survive a page reload; UHID 0 handling; payment popup themed. |
| earlier | Booking wizard, CreateAppointment / GetAppointmentByID, languages (en / hi / bn), security hardening, one-time step URLs, themes. |
