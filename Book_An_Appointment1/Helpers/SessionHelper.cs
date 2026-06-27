using Book_An_Appointment1.ViewModule;
using System.Text.Json;
namespace Book_An_Appointment1.Helpers
{
    public class SessionHelper
    {
        private const string AppointmentKey = "AppointmentData";
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
    }
}
