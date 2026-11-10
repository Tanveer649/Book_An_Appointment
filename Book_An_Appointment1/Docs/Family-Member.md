# Add family member (registered patient)

After a registered patient verifies their mobile with OTP, the family members on that number are
shown as cards. Below the cards there is now **+ Add family member**.

## Flow

1. The patient clicks **Add family member** (mobile already verified) → a form opens: title, first / middle / last name,
   date of birth, gender, state, city (required) and address, email, pincode (optional).
   Choosing the title sets the gender, as on the new-patient form.
2. **Save & select** → `?handler=AddFamilyMember` → hospital API
   `POST /api/VCP/MobSaveRegistrationForPatient` with `IsFamilyMember = 1` (number).
3. On success the new member is added to the list **and selected**, a short highlight shows the new
   card, and the patient continues to Review & Confirm as usual. UHID of a new member is `0`
   (account created, no appointment yet); `GuestPateintId` from the API is used for the booking.
4. The member stays in the list after a reload / Back (it is in the session).

## OTP

No second OTP is asked from the patient. The API checks **mobile + OTP**; the server handles it:

1. It sends the OTP from the mobile verification — the `otp` value of the VerifyPatientOTP response when
   the API returns one, otherwise the OTP the patient typed. Kept on the server only (session key
   `Booking.FamilyOtp`), never sent to the browser, removed when the booking ends.
2. The verification uses that OTP up, so the API may answer *"OTP Or Mobile No is incorrect!"* (error
   text matching `FamilyMemberSettings:OtpErrorPattern`). Then the server asks `GenerateOTPForPatient` for a
   fresh OTP for the **same verified mobile**, reads the number from its response (`Data[].OtpNo`) and
   retries once. (The patient may receive that SMS, but does not have to type it.)
3. Only if that also fails (e.g. the OTP API does not return the number) the form says
   *"The OTP has expired. Please verify the mobile number again…"*. The log line
   `AddFamilyMember — OTP API returned no OTP number` shows this case.

## Checks (browser and server)

- Names: letters, spaces, `'` and `-` only (max 50); DOB in the past (max 120 years); gender
  Male / Female / Other; email and 6-digit pincode when given; address max 200 characters.
- The same person (same first + last name + DOB) already on the number → *"This family member is
  already in the list — please select their card."*
- Rate limited like booking (`RateLimits:Booking` per visitor per window).

## API mapping

| Payload field | Value |
|---|---|
| `inyHospitalLocationId` | **number** — doctor's hospital location (session), else `ApiParam:GetFacilitiesParams:HospitalLocationId` |
| `FacilityID` | facility chosen on step 1 |
| `FirstName` / `MiddleName` / `LastName` | form (`MiddleName` = `" "` when empty, as in the API sample) |
| `DOB` | `yyyy-MM-dd` |
| `Gender` | code from `AppointmentApi:GenderCodes` |
| `Mobile` | the verified mobile |
| `TitleId` | from the title list |
| `CountryId` / `StateId` / `CityId` | default country / form |
| `AreaId` | `FamilyMemberSettings:DefaultAreaId` (**default `"0"` — please confirm with the API team**) |
| `ZipCode`, `Address`, `Email` | form (empty when not given) |
| `PaymentFlag` | `FamilyMemberSettings:PaymentFlag` (`"0"`) |
| `UniqueValue` | `""` |
| `OTP` | the OTP the mobile was verified with |
| `IsFamilyMember` | `1` — **a number, not the string `"1"`** (see below) |

> **Why numbers:** with `"IsFamilyMember": "1"` the SQL profiler showed `@bitIsFamilyMember=0` and the
> member was not added. The API reads this field into a bool, and the string `"1"` cannot be converted to a
> bool, so the value is silently dropped (false = 0). The number `1` converts correctly whether the API
> property is bool, int or string. `inyHospitalLocationId` is sent as a number for the same reason.
> To see exactly what is sent, set `"Logging:LogLevel:Book_An_Appointment1.Services.Implementations.PatientRegistrationService": "Debug"`
> — the request body is logged with the OTP masked.

The success response repeats fields in two spellings (`"firstName": "Subarna"` and
`"FirstName": null`). The portal reads the **exact lower-case names** so the empty duplicates do not
overwrite the real values (`Services/Implementations/PatientRegistrationService.cs`).

## Development

With the Development test OTP (`123456`) the API is **not** called — a member is simulated, so
nothing is created in the hospital system while testing.

## Code

- `PatientDetailsModel.OnPostAddFamilyMemberAsync` / `ValidateFamilyMember`
- `Services/Implementations/PatientRegistrationService.cs`, `Models/PatientOTP/FamilyMemberRegistration.cs`
- `patient-details.js` → `initFamilyMemberForm`; markup `#familyModal` in `PatientDetails.cshtml`
