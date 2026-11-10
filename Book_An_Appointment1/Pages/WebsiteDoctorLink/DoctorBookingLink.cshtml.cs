using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Security;
using Book_An_Appointment1.Services.Interfaces;
using Book_An_Appointment1.ViewModule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Book_An_Appointment1.Pages.WebsiteDoctorLink
{
    // Doctor link from the hospital website / doctor profile page:
    //   /book/doctor/{doctorId}?facilityId=1[&date=2026-11-06][&slot=2026-11-06 09:30]
    // Starts a fresh booking with hospital, speciality and doctor already chosen → the patient only picks a slot.
    // facilityId / specialityId are optional: the doctor record from the hospital API is the source of truth.
    // Unknown doctor → normal booking page with a short message. See Docs/Doctor-Link.md.
    public class DoctorBookingLinkModel : PageModel
    {
        private readonly IDoctorDirectory _directory;
        private readonly ILogger<DoctorBookingLinkModel> _logger;
        private readonly IConfiguration _configuration;

        public DoctorBookingLinkModel(IDoctorDirectory directory, ILogger<DoctorBookingLinkModel> logger, IConfiguration configuration)
        {
            _directory = directory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync(int doctorId, int? facilityId, int? specialityId, string? date, string? slot)
        {
            var match = await _directory.FindAsync(doctorId, facilityId);

            // Any booking in progress in this browser is replaced by the one for this doctor
            SessionHelper.ClearAppointmentData(HttpContext.Session);

            if (match == null || match.SpecialityId <= 0)
            {
                HttpContext.Session.SetString(SessionHelper.NoticeKey, "Doctor.LinkNotFound");
                return this.GoToStep(BookingStep.Appointment);
            }

            if (specialityId is > 0 && specialityId != match.SpecialityId)
                _logger.LogInformation("Doctor link speciality {S} differs from the doctor's ({D}) — using the doctor's", specialityId, match.SpecialityId);

            var (slotDate, slotTime) = ReadSlot(date, slot);

            SessionHelper.SetAppointmentData(HttpContext.Session, new AppointmentSessionData
            {
                FacilityId = match.FacilityId,
                SpecialityId = match.SpecialityId,
                DoctorId = match.Doctor.DoctorId,
                HospitalLocationId = match.Doctor.HospitalLocationId,
                // Optional date / slot from the website: only pre-selected — the booking page shows it only when
                // the doctor really has that slot free, and Continue checks it again against the live slot list
                SlotDate = slotDate,
                SlotTime = slotTime
                // name, hospital and fees are filled by the booking page (AppointmentModel.SaveDoctorToSession)
            });

            _logger.LogInformation("Doctor link → booking | DoctorId={D} FacilityId={F} SpecialityId={S} Date={Date} Slot={Slot}",
                match.Doctor.DoctorId, match.FacilityId, match.SpecialityId, slotDate, slotTime);

            return this.GoToStep(BookingStep.Appointment);
        }

        // date = yyyy-MM-dd (opens that day), slot = "yyyy-MM-dd HH:mm" or "HH:mm" with date (pre-selects that slot).
        // Past days, days beyond the booking window (MaxAppointmentAllowedDays) and bad formats are ignored.
        private (string Date, string Time) ReadSlot(string? date, string? slot)
        {
            DateTime? day = DateTime.TryParseExact(date?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
            DateTime? start = null;
            var s = slot?.Trim() ?? "";
            if (DateTime.TryParseExact(s, new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var full))
                start = full;
            else if (day != null && TimeSpan.TryParseExact(s, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
                start = day.Value.Add(time);

            day ??= start?.Date;
            var lastDay = DateTime.Today.AddDays(_configuration.GetValue("MaxAppointmentAllowedDays:Days", 90));
            if (day == null || day < DateTime.Today || day > lastDay) return ("", "");
            if (start == null || start.Value.Date != day || start <= DateTime.Now) return (day.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "");

            // same text format as the slot API ("2026-11-05 08:30") so the booking page can match it
            return (day.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), start.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        }
    }
}
