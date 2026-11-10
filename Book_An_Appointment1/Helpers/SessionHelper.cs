using Book_An_Appointment1.ViewModule;
using System.Text.Json;
namespace Book_An_Appointment1.Helpers
{
    public class SessionHelper
    {
        private const string AppointmentKey = "AppointmentData";

        // Patient type the OTP API pointed to ("new" / "registered") — the only switch Patient Details allows
        public const string AllowedTypeSwitchKey = "Booking.AllowedTypeSwitch";

        // OTP the registered mobile was verified with — server side only, never sent to the browser.
        // "Add family member" passes it to MobSaveRegistrationForPatient (the API checks mobile + OTP).
        public const string FamilyOtpKey = "Booking.FamilyOtp";

        // One-time message for the booking page (e.g. a doctor link that no longer works) — translation key
        public const string NoticeKey = "Booking.Notice";

        public static void SetAppointmentData(
            ISession session, AppointmentSessionData data)
        {
            session.SetString(AppointmentKey, JsonSerializer.Serialize(data));
        }
        public static AppointmentSessionData? GetAppointmentData(ISession session)
        {
            var json = session.GetString(AppointmentKey);
            return json == null
                ? null
                : JsonSerializer.Deserialize<AppointmentSessionData>(json);
        }
        // Booking finished — wipe wizard data so Back/refresh cannot book the same slot again
        public static void ClearAppointmentData(ISession session)
        {
            session.Remove(AppointmentKey);
            session.Remove("Booking.SlotEnd");
            session.Remove(AllowedTypeSwitchKey);
            session.Remove(FamilyOtpKey);
            Security.BookingFlow.Reset(session);   // old /book/{token} URLs stop working
        }
    }
}
