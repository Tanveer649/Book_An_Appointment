# Deploying the portal for a new client (hospital)

Every hospital runs the **same build**. Publishing, installing and **updating** the portal on IIS: [Deployment.md](Deployment.md).

What changes per client:

1. `appsettings.json` (section 2 below)
2. Branding files in `wwwroot` — logo, hospital photos, theme colours (section 3)
3. A few hospital-specific texts in the language files (section 4)
4. Server set-up — IIS, HTTPS, keys folder (sections 5 and 6)

Work through this page top to bottom; section 8 is the final checklist.

> ### Short answers
> - **Do I need to set the environment?** No. A published (Release) build runs as **Production**
>   automatically. The `Development` setting only lives in `Properties/launchSettings.json`, which Visual Studio
>   uses on your PC — it is not used on the server. Just make sure nobody sets
>   `ASPNETCORE_ENVIRONMENT=Development` on the server (that would switch off real OTP SMS).
> - **Is `SlipSettings:PublicBaseUrl` the API URL?** **No — never put the API URL there.** It is the address of
>   **this portal** as patients open it (e.g. `https://booking.hospital.com`). Normally **leave it empty**; the
>   portal then uses the address the patient used. Set it only if the portal sits behind a proxy / load balancer
>   and the QR codes come out with an internal address (test: scan the poster at `/booking-qr`).
> - **Do I need to do anything for App_Data/keys?** **No.** The folder is created automatically on first start.
>   If the server does not let the site write there, the portal still runs (the log shows a warning) — only
>   slip links / QR codes may stop working after an app-pool restart. Section 6 has the optional improvements.

---

## 1. Before you start — collect from the client

| Item | Example | Used for |
|---|---|---|
| HIS API address + portal API user / password | `http://10.0.0.5:2525`, user, password | `ApiSettings` |
| Hospital name, emergency number, footer phone numbers | "Ruby General Hospital", +91 98765 43210 | header / footer |
| Logo file (PNG with transparent background or SVG) | `ruby-logo.png` | header, slip poster |
| 3–5 hospital photos | building, reception, ward | first page carousel |
| Brand colours (primary + accent) | `#8B1E3F`, `#D97706` | theme file |
| Public portal address | `https://booking.rubyhospital.com` | QR codes, website links |
| Payment gateway URL | `https://pay.rubyhospital.com/pay` | Pay now |
| Facility code / hospital location id in the HIS | `3`, `1` | `ApiParam` |
| Terms / privacy / refund / about-us page links | `https://rubyhospital.com/terms` | footer |
| Languages wanted | English + Bengali | `LanguageSettings` |

---

## 2. appsettings.json — what to change

Legend: **Must** = has to be set for every client · *Check* = keep the default unless the client differs · Keep = leave as is.

### 2.1 Hospital API — **Must**

```jsonc
"ApiSettings": {
  "BaseUrl": "http://<client-api-host>:<port>",   // HIS API of this client
  "UserName": "<portal api user>",
  "Password": "<portal api password>",
  "TimeoutSeconds": 30
}
```

If UserName / Password are empty the log shows a **Critical** message at start and nothing loads.

### 2.2 Hospital branding — **Must**

```jsonc
"HospitalSettings": {
  "HospitalName": "Ruby General Hospital",     // header, PDF slip, wall poster
  "LogoPath": "/images/Logo.png",              // file in wwwroot (see 3.1)
  "LogoHeight": "56px",                        // shown 32–60 px high whatever you enter (see 3.1)
  "LogoWidth": "auto",                         // keep "auto" — width follows the logo, max 200 px
  "HeaderText": "Book Appointment",            // centre title of the header
  "EmergencyNumber": "+91 98765 43210",
  "DisclaimerText": "…",                       // footer disclaimer (English; see section 4)
  "FooterPhone1": "…", "FooterPhone2": "…", "FooterPhone3": "…",
  "AboutUsUrl": "https://…/about-us",
  "TermsUrl": "https://…/terms",
  "RefundPolicyUrl": "https://…/refund-policy",
  "PrivacyPolicyUrl": "https://…/privacy-policy"
}
```

`HospitalFontSize` / `HeaderFontSize` are not used any more (sizes are responsive) — leave them.

### 2.3 HIS parameters — **Must** (ask the HIS / API team)

```jsonc
"ApiParam": {
  "GetFacilitiesParams":   { "FacilityCode": "1", "HospitalLocationId": "1" },   // FacilityCode = DEFAULT facility on the first page
  "DefaultLocationSettings": { "DefaultCountryId": "1", "DefaultCountryName": "INDIA" },
  "GetSpecialitiesParams": { "HospitalLocationId": "1", "isTeleconsultation": "0" },
  "GetDoctorsParams":      { "facilityCode": "1", "doctorId": "0", "specializationId": "0", "isTeleconsultation": "0" },
  "GetConsultationParams": { … keep … },
  "GetDoctorSlotsParams":  { … keep … }
}
```

`GetFacilitiesParams:FacilityCode` also chooses the facility that is **pre-selected** when the first page opens: the
facility whose id (or facility code, if the API returns one) equals this value. If none matches and the hospital has
only one facility, that one is selected. Patients can still pick another facility from the dropdown.

### 2.4 Booking API values — *Check* with the HIS team

```jsonc
"AppointmentApi": {
  "Source": "WA", "AppointmentType": "RC", "SourceAppointmentId": "12382245",
  "DefaultSlotMinutes": 15,
  "GenderCodes": { "Male": "1", "Female": "2", "Other": "3" }   // codes of THIS HIS
}
```

### 2.5 Payment — **Must**

```jsonc
"PaymentSettings": {
  "EnablePayNow": true,
  "EnablePayOnArrival": true,
  "PaymentGatewayUrl": "https://<client gateway>/path"   // gets ?appointmentId=… appended
}
```

### 2.6 Theme — **Must**

```jsonc
"ThemeSettings": { "Theme": "ruby" }   // loads wwwroot/themes/ruby.css (see 3.3)
```

### 2.7 Languages — *Check*

```jsonc
"LanguageSettings": {
  "DefaultLanguage": "en",
  "Languages": [ { "Code": "en", … }, { "Code": "bn", …, "Font": "Noto Sans Bengali" } ]
}
```

Remove the languages the client does not want. Adding a new one: [Language-Feature.md](Language-Feature.md).

### 2.8 Public address, slip and QR — *Check* (usually leave `PublicBaseUrl` empty)

```jsonc
"SlipSettings": {
  "PublicBaseUrl": "",   // THIS portal's public address (NOT the API URL); empty = address the patient used
  "LinkValidDays": 30, "ArriveMinutesBefore": 15, "ReminderMinutesBefore": 60,
  "TimeZone": "Asia/Kolkata",
  "EnablePrint": false, "EnableViewSlip": false, "EnableAddToCalendar": false   // buttons on/off per client
},
"BookingQrSettings": {
  "Enabled": true,
  "Url": "",          // empty = PublicBaseUrl + /book
  "Title": "", "Subtitle": ""
}
```

Leaving `PublicBaseUrl` empty is right for most servers. Set it (e.g. `"https://booking.rubyhospital.com"`) only when the
portal is behind a proxy / load balancer and the QR codes show an internal address. **Never** the HIS API address.

### 2.9 Data Protection keys — optional (see section 6)

```jsonc
"DataProtection": { "KeysPath": "" }   // empty = App_Data/keys, created automatically. Optional: a folder outside the site
```

### 2.11 Hide facilities / doctors / specialities

When the client says "do not show this facility / doctor / speciality online":

```jsonc
"HideList": {
  "Enabled": true,
  "FacilityIds": [ "2" ],        // facility ids (GetFacilityDetails "facilityId") — also hides their specialities and doctors
  "DoctorIds": [ "18909" ],      // doctor ids from the HIS (GetDoctorList "id"), as strings
  "SpecialityIds": [ "103" ]     // speciality ids (GetSpecialityList "id") — also hides all their doctors
}
```

Hidden = not in the Facility / Speciality / Doctor dropdowns, doctor links and profile pages say "not found", and Continue
refuses them. `Enabled: false` switches the list off without deleting the ids. Changes apply immediately (no restart).

### 2.12 Other feature switches

| Setting | Default | What |
|---|---|---|
| `AppointmentPageSettings:EnableStartOver` | true | "Start over" link on the first page |
| `AppointmentPageSettings:EnableNextAvailable` | true | Day without free slots → "Next free day is …" + **Go to …** button |
| `FindSlipSettings:Enabled` | true | Find my slip page `/find-slip` + link on the first page |
| `SlipSettings:EnablePrint / EnableViewSlip / EnableAddToCalendar` | false | Buttons in the booked popup / slip page |
| `BookingQrSettings:Enabled` | true | Wall poster `/booking-qr` |

### 2.10 Usually keep

| Section | Default | Change when |
|---|---|---|
| `OtpSettings` | length 6, patterns | the HIS uses another OTP length / error texts |
| `RateLimits` | 10 min window, 5 OTP sends, 10 verifies, 10 bookings per IP | many patients share one IP (e.g. hospital Wi-Fi kiosk) → raise |
| `MaxAppointmentAllowedDays` | 90 | client allows a different booking window |
| `FamilyMemberSettings` | AreaId "0" | the HIS needs a real area id |
| `Logging` | Information | — |

---

## 3. Branding files (wwwroot)

### 3.1 Logo

| | Recommended |
|---|---|
| Format | **PNG with transparent background** (or SVG) |
| Shape | horizontal or square; widest useful ratio ≈ **3.5 : 1** (e.g. 420 × 120) |
| Pixel size | at least **2× the shown size** for sharp phones: height **≥ 112 px** (e.g. 400 × 112, or 200 × 200 for a square logo) |
| File size | < 200 KB |
| Location | `wwwroot/images/` → set `HospitalSettings:LogoPath` to `/images/<file>` |

How it is shown (any size you upload is fitted, never stretched or cropped):

| Screen | Logo height | Max width |
|---|---|---|
| Desktop | `LogoHeight` kept between **32 and 60 px** (recommended 56px) | 200 px |
| Tablet (≤ 1000 px) | up to 52 px | 160 px |
| Phone | 42 px | 120 px |
| Small phone (≤ 380 px) | 36 px | 96 px |

A logo with a lot of empty border looks small — crop the empty space before uploading.
If the file is missing, the logo is hidden and only the hospital name shows.

### 3.2 Hospital photos (first page carousel)

`wwwroot/images/HospitalImages/img1.png … img5.png` — **1600 × 1000 px (16:10)**, JPG/PNG < 400 KB each.
Missing files are skipped automatically; with no photos the carousel is hidden.

### 3.3 Theme colours

Copy `wwwroot/themes/aiims.css` to `wwwroot/themes/<client>.css` (lowercase, letters / numbers / `-`),
change the 4 colours, set `ThemeSettings:Theme`. Or use one of the 20 ready themes `a`–`t`.
Details: [Theme.md](Theme.md). Deploy only the theme file(s) this client uses.

---

## 4. Hospital-specific texts in the language files

`Resources/Languages/{en,hi,bn}.json` contain a few texts that name the hospital's own branches or rules.
Check them for every client:

| Key | What |
|---|---|
| `Footer.PanelPatients` | panel-patient phone line ("Sector 14: {0}, Sector 56: {1}, …") — branch names |
| `Footer.DisclaimerText` | translated disclaimer (English comes from `HospitalSettings:DisclaimerText`) |
| `Header.Title` | translated header title |
| `Review.ArrivalNote`, `Slip.ArriveNote` | arrival / payment instructions |

---

## 5. Server set-up (IIS)

1. Install the **.NET 10 Hosting Bundle** on the server.
2. Publish the project (Release) and copy it to the site folder.
3. IIS site → application pool *No Managed Code*.
4. Environment: **nothing to do** — a published build runs as Production by default (the start-up log shows
   `Hosting environment: Production`). Never set `ASPNETCORE_ENVIRONMENT=Development` on a server — that switches off real OTP SMS.
5. HTTPS binding with a valid certificate (the portal redirects http → https).
6. Recommended: give the app-pool user **write** access to `App_Data` (section 6).
7. PDF slip fonts: Windows servers already have Arial. On Linux install DejaVu or Liberation fonts.

---

## 6. Data Protection keys — one set per client

**If you do nothing it works.** On first start the portal creates `App_Data/keys` itself. If IIS does not let
the site write there, the portal still starts and uses ASP.NET's default key storage; the log then shows
*"Data Protection keys folder … is not writable"*. In that case slip links / QR codes already sent may stop
working after an app-pool restart — fix it by giving the app-pool user write access to `App_Data`.

**Question:** with many clients, should the key be declared in appsettings.json, or is the App_Data folder fine?

**Answer:** the folder is the right approach — and it already gives every client its own keys:

- Each client is a separate deployment (own site folder / server), so each gets its **own** `App_Data/keys`
  created automatically on first start. Client A's slip links can never be opened with client B's keys.
- **Do not put the key itself in appsettings.json.** The key file is a secret that ASP.NET also rotates
  every 90 days; appsettings only holds the **folder path** (`DataProtection:KeysPath`).
- **Never copy one client's keys to another client** (and never commit them — the folder is in `.gitignore`).

When to set `DataProtection:KeysPath`:

| Situation | KeysPath |
|---|---|
| One server, keys folder never deleted on redeploy | leave empty → `App_Data/keys` in the site folder |
| **Recommended for production** | a folder **outside** the site folder, e.g. `D:\PortalKeys\<client>` — a "delete all files" publish can then never wipe it |
| Several servers for the same client (load balancer) | **one shared folder** for all of them, e.g. `\\fileserver\PortalKeys\<client>` |
| Several clients on the same server | a **different** folder per client |

If the keys are lost: slip links / QR codes already sent stop working ("invalid link") and patients in the
middle of a booking have to start again. Nothing else breaks.

---

## 7. After deployment — tell the client

| Who | What |
|---|---|
| Hospital website team | [Doctor-Link.md](Doctor-Link.md) — `https://<portal>/book/doctor/{doctorId}?facilityId={id}` (+ optional `date`, `slot`) |
| Hospital admin | Wall poster: open `https://<portal>/booking-qr` → **Print poster** (A4) or download PNG / SVG for a print shop |
| Reception | Patients show the appointment slip (PDF / QR) at the registration counter |

---

## 8. Go-live checklist

- [ ] `ApiSettings` BaseUrl / UserName / Password set — no Critical message in the log at start
- [ ] Start-up log shows `Hosting environment: Production` (default for a published build)
- [ ] `HospitalSettings` name, emergency number, footer phones and links
- [ ] Logo uploaded (≥ 112 px high, transparent), looks right on desktop and phone
- [ ] Hospital photos (or none), theme file present and `ThemeSettings:Theme` set
- [ ] `ApiParam` facility / location ids — facilities, specialities, doctors load on `/book`
- [ ] `AppointmentApi:GenderCodes` match the HIS
- [ ] `PaymentSettings:PaymentGatewayUrl` set; Pay now redirects correctly
- [ ] Scan the poster QR at `/booking-qr` with a phone — it opens the portal (if it shows an internal address, set `SlipSettings:PublicBaseUrl` to the **portal** address, not the API)
- [ ] Start-up log shows `Data Protection keys folder: …` (no "not writable" warning); shared folder if load-balanced
- [ ] Language files: `Footer.PanelPatients` and other hospital texts checked
- [ ] Real test: OTP SMS arrives (new + registered), one Pay-on-arrival booking, slip PDF downloads, QR opens the slip
- [ ] Doctor link works: `https://<portal>/book/doctor/<a doctor id>?facilityId=<id>`
- [ ] Checklist in [Security.md](Security.md#10-before-go-live-checklist)
