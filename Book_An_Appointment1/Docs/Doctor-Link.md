# Doctor links — for the hospital website team

The booking portal can be opened **with a doctor already selected**. When a visitor clicks
"Book an appointment" for a doctor on the hospital website, the portal opens with the hospital,
speciality and doctor filled in. The patient only picks a date/slot and continues as usual.

## 1. What the website must link to

```
https://<portal-address>/book/doctor/{doctorId}?facilityId={facilityId}&date={yyyy-MM-dd}&slot={HH:mm}
```

| Part | Required | What it is |
|---|---|---|
| `{doctorId}` | **Yes** | The doctor's **id** in the hospital system (HIS) — the `id` field of `GetDoctorList` (e.g. `18908`). Same id the website already uses for the doctor's profile. |
| `facilityId` | Recommended | Hospital / facility id (the `facilityId` of `GetFacilityDetails`, e.g. `1`). Needed when one portal serves **several** facilities; with a single facility it may be left out. |
| `date` | Optional | Day to open, `yyyy-MM-dd` (e.g. `2026-11-06`). Past days / days beyond the booking window are ignored (today opens). |
| `slot` | Optional | Time the patient clicked on the website, `HH:mm` 24-hour (e.g. `09:30`) together with `date`, or full `yyyy-MM-dd HH:mm`. Pre-selected **only if the doctor really has that slot free**; otherwise the day opens with the message *"The selected time is no longer available. Please choose another slot."* |
| `specialityId` | No | Not needed — the portal reads the doctor's speciality from the HIS. If sent and it does not match, the HIS value is used. |

**Examples** (Dr. Animesh Kumar Mishra, id 18908, facility 1):

```html
<!-- doctor only: patient picks day + time in the portal -->
<a href="https://booking.your-hospital.com/book/doctor/18908?facilityId=1">Book an appointment</a>

<!-- doctor + day -->
<a href="https://booking.your-hospital.com/book/doctor/18908?facilityId=1&date=2026-11-06">Book for 6 Nov</a>

<!-- doctor + day + time the patient clicked on the website -->
<a href="https://booking.your-hospital.com/book/doctor/18908?facilityId=1&date=2026-11-06&slot=09:30">09:30 AM</a>
```

The patient then only checks the pre-selected slot and clicks **Continue** (the slot is checked again
against the doctor's live slots, so a slot taken in the meantime can never be booked twice).

So the website team only has to pass the **doctor id** (and the facility id), optionally the day and
time. Nothing else — no patient data, no secret keys, no API access.

## 2. Doctor profile page (optional)

Every doctor also has a public, shareable profile page in the portal:

```
https://<portal-address>/doctor/{doctorId}?facilityId={facilityId}
```

It shows photo, qualification, speciality, department, hospital, consultation fee, next available
time, treatments and awards (whatever the HIS has), with a **Book appointment** button (goes to the
link in section 1) and a **Share profile** button (phone share sheet / copy link). Link previews
on WhatsApp / Facebook show the doctor's name, speciality and photo.

The website can link either to the profile page or straight to the booking link.

## 3. What happens when…

| Situation | Result |
|---|---|
| Doctor id is valid | Booking page opens with hospital, speciality and doctor selected and today's slots shown. |
| Doctor id unknown / doctor not available online | Normal booking page with the message *"The doctor from that link is not available for online booking right now. Please choose a doctor below."* |
| Visitor was in the middle of another booking in the same browser | That booking is replaced by the one for the linked doctor. |
| Profile link with an unknown doctor | Friendly "Doctor not found" page (HTTP 404) with a **Find a doctor** button. |

## 4. Notes for the portal team

- Doctor look-ups are cached for 10 minutes (missing doctors for 2 minutes), so traffic from the
  website does not call the hospital API for every click. A doctor changed in the HIS shows up within that time.
- Code (folder `Pages/WebsiteDoctorLink/` = everything the hospital website links to):
  `DoctorBookingLink.cshtml(.cs)` (the `/book/doctor/{id}` link), `DoctorProfile.cshtml(.cs)` (the `/doctor/{id}` profile),
  `Services/Implementations/DoctorDirectory.cs` (look-up + cache), `AppointmentModel.SaveDoctorToSession`.
- The link only pre-selects the doctor; the slot is still checked against the doctor's live slots
  when the patient clicks Continue, exactly like a normal booking.
