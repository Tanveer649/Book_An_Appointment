# Security & hardening

What protects the Book Appointment portal, where it lives in the code, and what has to be set up on a server.

---

## 1. Setup — per client deployment

Each client gets the same build; only **appsettings.json → ApiSettings** changes:

```json
"ApiSettings": {
  "BaseUrl": "http://<client-api-host>:<port>",
  "UserName": "<client user>",
  "Password": "<client password>",
  "TimeoutSeconds": 30
}
```

On the server also set `ASPNETCORE_ENVIRONMENT=Production` (IIS: `web.config` → `environmentVariables`).

If UserName / Password are empty, the log shows a **Critical** message at startup and API calls fail.

> Optional: environment variables `ApiSettings__UserName` / `ApiSettings__Password` on a server override
> the values in appsettings.json (built-in ASP.NET Core behaviour) — useful if a client does not want the
> password inside a file.

---

## 2. Environment matters

| | Development (your PC) | Production / any other environment |
|---|---|---|
| Send OTP | No SMS is sent | Real API (`GenerateOTPForSignup` / `GenerateOTPForPatient`) |
| Verify OTP | `123456` accepted; registered patient gets the test family | Real API only |
| Errors | Developer exception page | Friendly `/error` page, HSTS on |

`ASPNETCORE_ENVIRONMENT` must **not** be `Development` on the server. Code: `PatientDetails.cshtml.cs` (`_useTestOtp`, `TestOtp`).

---

## 3. URLs — page name + one-time ID, buttons only

| Step | URL |
|---|---|
| Select doctor & slot | `/appointment/{id}` |
| Patient details | `/patientdetails/{id}` |
| Review & confirm | `/reviewconfirm/{id}` |
| Error / not found | `/error` |

`{id}` = **64 random characters** (384-bit), e.g.
`/patientdetails/0Ut9YacCpI6A2K23SPf3IgoloxDEQFnTXgnMSA7qF485J_2EhQJn9jehRa30o99W`.
Code: `Security/BookingFlow.cs` (`BookingFlow`, `GoToStep`, `BookingFlowMiddleware`).

| Rule | Behaviour |
|---|---|
| One valid URL | The session stores only the current step + its id. The URL works only if **both** match |
| Moving | Only on the server, through page buttons: Continue, Review & Confirm, ← Back, Edit (`this.GoToStep(...)`). Every move issues a **new id** — all older URLs stop working |
| Back / Edit buttons | Small POST forms with an anti-forgery token (`OnPostBack` on Patient Details and Review) — not links |
| Changed page name, edited / old id, browser Back to an old URL | Lands on the current step |
| Internal routes / old addresses (`/patient-details`, `/PatientDetails/PatientDetails`, …), `/`, `/book` | Land on the current step |
| Caching | Step pages are `no-store`; a page restored from the browser's back/forward cache reloads → current step |
| Booking finished | Flow is reset (`SessionHelper.ClearAppointmentData`) — the next visit starts at step 1 |

- Patient type, doctor, slot, OTP state are kept in the **server session** — no query parameters.
- In code, change step only with `return this.GoToStep(BookingStep.X);` (never `RedirectToPage`/`asp-page` for the steps).
- The Razor Page routes (`/book-appointment`, `/patient-details`, `/review-confirm`) are internal only.
- The flow needs the session cookie; with cookies blocked the browser reports "too many redirects".

---

## 4. What the server checks (never trusted from the browser)

| Check | Where |
|---|---|
| Doctor/facility on Continue must be the one loaded in this session | `Appointment.cshtml.cs` → `OnPostContinueAsync` |
| Slot must be a **free slot from the hospital API** for that doctor and date, not in the past; slot end time is taken from the API | `FindAvailableSlotAsync` |
| Patient type comes from session (chosen on page 1), not the posted form | `PatientDetails.cshtml.cs` → `OnPost` |
| Mobile verified = OTP verified **in session for the same number** (hidden field alone is not enough) | `ValidateNewPatient`, `ValidateRegisteredPatient` |
| Registered patient must be one of the family members returned for the verified number | `OnPostSelectRegisteredPatient` |
| Mobile (10 digits) and OTP (digits, `OtpSettings:OtpLength`) validated before calling the API | `IsValidMobile`, `IsValidOtp` |
| Slot date format validated before calling the slot API | `OnGetSlotsByDateAsync` |
| Booking data re-validated before CreateAppointment | `ReviewAndConfirmation.cshtml.cs` → `ValidateBookingRequest` |
| OTP is never returned to the browser or stored in session | `OnPostVerifyRegisteredOtpAsync` |
| Anti-forgery token on every POST (Razor Pages default) | forms + `RequestVerificationToken` header in AJAX |

---

## 5. Rate limiting

Per visitor IP, configurable in `appsettings.json`:

```json
"RateLimits": { "WindowMinutes": 10, "OtpSend": 5, "OtpVerify": 10, "Booking": 10 }
```

| Policy | Handlers |
|---|---|
| OtpSend | `SendOtp`, `SendRegisteredOtp` |
| OtpVerify | `VerifyOtp`, `VerifyRegisteredOtp` |
| Booking | `CreateAppointment` |

Over the limit → HTTP 429 with `{ success:false, message }` (translated) — the page shows it like any other error.
Code: `Security/RateLimitingSetup.cs`. Counters are in memory (reset on app restart).

> Behind a reverse proxy / load balancer every visitor may appear with the proxy's IP — then add
> `ForwardedHeaders` middleware so the real client IP is used.

---

## 6. Browser security headers

`Security/SecurityHeadersMiddleware.cs`, on every response:

| Header | Value / purpose |
|---|---|
| Content-Security-Policy | Scripts/styles only from this site, cdn.jsdelivr.net, code.jquery.com, Google Fonts; no plugins; no framing; forms only post to this site |
| X-Frame-Options | `DENY` — page cannot be put in an iframe (clickjacking) |
| X-Content-Type-Options | `nosniff` |
| Referrer-Policy | `strict-origin-when-cross-origin` — full URLs never leak to other sites |
| Permissions-Policy | camera, microphone, location, payment, USB disabled |
| Cross-Origin-Opener-Policy | `same-origin` |
| Server header | removed (Kestrel) |

CDN libraries are pinned to exact versions with **Subresource Integrity** hashes (`_Layout.cshtml`) — a modified
file on the CDN is refused by the browser. When upgrading a library, update the `integrity` value too.

**Known limitation:** CSP still allows inline scripts (`'unsafe-inline'`) because the pages use inline
`<script>` blocks and `onclick=""` attributes. Moving those into the `.js` files would allow a strict CSP.

---

## 7. Cookies & session

| Cookie | Settings |
|---|---|
| Session | HttpOnly, SameSite=Lax, Secure on HTTPS, 60 min idle |
| Anti-forgery | HttpOnly, SameSite=Strict, Secure on HTTPS |
| Language (`.AspNetCore.Culture`) | HttpOnly, SameSite=Lax, 1 year |

Session data is cleared after a successful booking (`SessionHelper.ClearAppointmentData`).

---

## 8. Logging & personal data

- Mobile numbers are masked in logs: `******3210` (`Helpers/LogMask.cs`).
- Full API response bodies (names, DOB, OTP) are logged only at **Debug** level (off by default).

---

## 9. Performance changes

| Change | Effect |
|---|---|
| One API token for the whole app (cached until 1 min before expiry, refresh lock) | Before: every visitor session requested its own token |
| Specialities cached 6 h per facility (like facilities); states/cities 24 h (like countries) | Fewer API calls; empty/failed answers are not cached |
| API timeout 30 s (`ApiSettings:TimeoutSeconds`) | Before: 100 s default |
| Doctor photo proxy uses `IHttpClientFactory` | Proper connection reuse |
| ~3,000 lines of commented-out JS/CSS/Razor removed | Smaller files, easier to read (old versions are in git history) |

---

## 10. Before go-live checklist

- [ ] `ASPNETCORE_ENVIRONMENT=Production` on the server
- [ ] `ApiSettings` (BaseUrl, UserName, Password) set for this client in appsettings.json
- [ ] HTTPS certificate installed (HTTPS redirect + HSTS are active outside Development)
- [ ] `AllowedHosts` in `appsettings.json` set to the real domain instead of `*`
- [ ] `PaymentSettings:PaymentGatewayUrl` set to the real gateway
- [ ] Hospital API reachable over HTTPS (currently `http://40.81.241.111:2525` — traffic incl. OTP is unencrypted)
- [ ] If hosted on more than one server: shared Data Protection keys + distributed session cache
