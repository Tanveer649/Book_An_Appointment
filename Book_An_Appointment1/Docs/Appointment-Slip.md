# Appointment slip, QR code and Add to calendar

After **Pay on arrival** the "Appointment booked" popup now has a slip section:

- **QR code** — scanning it with any phone opens the slip page, where the PDF can be downloaded.
  The registration counter can scan it too to find the appointment.
- **Download PDF** — A4 slip (hospital colour from the theme, appointment ID, date, time, QR,
  patient / doctor details, arrival note).
- **Print** — opens the slip page and the print dialog (prints only the slip, not the header/footer).
  **Off by default** (`SlipSettings:EnablePrint`); hidden in the popup and on the slip page when off.
- **View slip** — the slip web page (in the portal language). **Off by default** (`SlipSettings:EnableViewSlip`);
  the QR code still opens the slip page.
- **Add to calendar** — *Google Calendar* or *Apple / Outlook (.ics)* with a reminder.
  **Off by default** (`SlipSettings:EnableAddToCalendar`); when off it is hidden in the popup and on the
  slip page, and the .ics address returns 404.

The slip shows the **UHID right next to the appointment ID** (page and PDF). `GetAppointmentByID`
returns no UHID for patients whose UHID is 0, so the UHID chosen at booking is also sealed in the slip
link and used when the API has none.

## Find my slip

`https://<portal>/find-slip` (link also on the first page and on the "invalid slip link" page).
The patient enters the mobile number used for booking → OTP → sees the slips issued for that number in the last
`SlipSettings:LinkValidDays` days, newest first, each with **Download PDF** (and **View slip** when
`SlipSettings:EnableViewSlip` is on). Past appointments are marked "Past".

- The hospital API cannot list appointments by mobile, so the portal **remembers each slip it issues**
  (Pay on arrival) in `App_Data/slips/{appointmentId}.json`: appointment id, doctor, speciality, time, amount,
  UHID, **patient name** (so family members sharing one mobile can tell their slips apart — the hospital API often
  returns no name for an appointment), masked mobile and an **HMAC of the mobile** (random per-server secret) —
  never the full number. Records older than `LinkValidDays` are deleted automatically.
- Each found slip shows **patient name, UHID**, date and time, doctor and appointment ID.
- Only slips issued **after** this feature was switched on can be found.
- OTP: registered-patient OTP first; if the number is not registered, the signup OTP. Rate limited like the
  other OTPs. Development: test OTP `123456`, no SMS.
- Switch: `FindSlipSettings:Enabled` (false = page 404, no link, nothing remembered).
  `FindSlipSettings:StorePath` = other folder; several servers → one shared folder.
- Like `App_Data/keys`, `App_Data/slips` is not in the publish and is git-ignored; deleting it only forgets old slips.

## Slip link

```
https://<portal-address>/slip/{link}            page
https://<portal-address>/slip/{link}?handler=Pdf   PDF download
https://<portal-address>/slip/{link}?handler=Ics   calendar file
```

- `{link}` is **encrypted and signed** (ASP.NET Data Protection). It cannot be guessed, edited to
  another appointment ID, or read. It expires after `SlipSettings:LinkValidDays` (default 30 days).
- It works on any device without the booking session (that is what makes the QR code useful).
- The page shows the mobile number masked (`XXXXXX6293`), is never cached and is marked `noindex`.
- Details come live from `GetAppointmentByID` (cached 5 minutes per appointment), so a changed
  status shows on the slip.

## Settings (`appsettings.json`)

```jsonc
"SlipSettings": {
  "LinkValidDays": 30,          // how long the slip link / QR keeps working
  "ArriveMinutesBefore": 15,    // "please arrive N minutes before"
  "ReminderMinutesBefore": 60,  // calendar reminder (.ics); 0 = none
  "TimeZone": "Asia/Kolkata",   // hospital time zone (calendar times are converted from it)
  "PublicBaseUrl": "",          // set when the server is behind a proxy, e.g. "https://booking.hospital.com"
  "EnablePrint": false,         // "Print" button (popup + slip page)
  "EnableViewSlip": false,      // "View slip" button in the booked popup
  "EnableAddToCalendar": false  // Google Calendar + .ics in the popup and on the slip page
},
"DataProtection": {
  "KeysPath": ""                // empty = App_Data/keys in the site folder
}
```

## Deployment — important

1. **Keys folder.** Slip links are signed with the keys in `App_Data/keys` (or `DataProtection:KeysPath`).
   - The IIS app-pool user needs **write** access to that folder.
   - Do **not** delete it on redeploy — links already sent to patients would stop working.
   - Several servers behind a load balancer: point `DataProtection:KeysPath` of all of them to the
     **same shared folder**.
   - On Windows the key files are encrypted for the machine (DPAPI). The folder is in `.gitignore`.
2. **PublicBaseUrl.** The QR code must contain the address patients can open. If the app sees an
   internal address (reverse proxy / load balancer), set `SlipSettings:PublicBaseUrl`.
3. **PDF fonts.** The PDF uses Arial / Segoe UI (Windows) or DejaVu / Liberation Sans (Linux).
   If none is installed, set `SlipSettings:PdfFontRegular` / `PdfFontBold` to `.ttf` file paths.
   Without a font the PDF link returns 503 and the page still offers Print.

## Libraries

| Package | Version | Licence | Use |
|---|---|---|---|
| QRCoder | 1.8.1 | MIT | QR code (SVG on the page, vector squares in the PDF) |
| PDFsharp | **6.2.4 (pinned)** | MIT | PDF |

> ⚠ **Do not upgrade PDFsharp to 6.5 or later** — from 6.5 the licence is **AGPL-3.0**, which would
> require publishing this portal's source code. The version is pinned with `[6.2.4]` in the .csproj.

## Language

The slip **page** follows the portal language (en / hi / bn). The **PDF** is in English — it is
read at the registration counter, and PDF fonts cannot shape Devanagari / Bengali correctly.

## Code

- `Services/Implementations/AppointmentSlipService.cs` — link, QR, data, calendar
- `Helpers/SlipPdfBuilder.cs` — PDF layout
- `Pages/Slip/Slip.cshtml(.cs)`, `wwwroot/css/slip.css`, `wwwroot/js/pages/slip.js` — slip page
- `ReviewAndConfirmationModel.AttachSlip` + `review-confirm.js` (`fillSlip`) — booked popup
