using Book_An_Appointment1.Models.DoctorItem;
using Book_An_Appointment1.Models.Facility;
using Book_An_Appointment1.Models.Speciality;

namespace Book_An_Appointment1.Helpers
{
    // Facilities / doctors / specialities the client does not want online (appsettings "HideList").
    //   "HideList": { "Enabled": true, "FacilityIds": [ "2" ], "DoctorIds": [ "18908" ], "SpecialityIds": [ "75" ] }
    // Hidden = not in the dropdowns, not reachable through doctor links / profiles, refused at Continue.
    // A hidden speciality also hides all its doctors; a hidden facility hides its specialities and doctors. Read on every call, so editing appsettings.json applies at once.
    public static class HideList
    {
        public static bool Enabled(IConfiguration config) => config.GetValue("HideList:Enabled", false);

        public static bool IsFacilityHidden(IConfiguration config, int facilityId) =>
            Enabled(config) && Contains(config, "HideList:FacilityIds", facilityId.ToString(System.Globalization.CultureInfo.InvariantCulture));

        public static List<FacilityItem> Filter(IConfiguration config, List<FacilityItem> facilities) =>
            Enabled(config) ? facilities.Where(f => !IsFacilityHidden(config, f.FacilityId)).ToList() : facilities;

        public static bool IsSpecialityHidden(IConfiguration config, string? specialityId) =>
            Enabled(config) && Contains(config, "HideList:SpecialityIds", specialityId);

        public static bool IsDoctorHidden(IConfiguration config, string? doctorId, string? specialityId) =>
            Enabled(config) && (Contains(config, "HideList:DoctorIds", doctorId) ||
                                Contains(config, "HideList:SpecialityIds", specialityId));

        public static bool IsDoctorHidden(IConfiguration config, DoctorItem doctor) =>
            IsDoctorHidden(config, doctor.Id, doctor.SpecialisationId);

        public static List<SpecialityItem> Filter(IConfiguration config, List<SpecialityItem> specialities) =>
            Enabled(config) ? specialities.Where(s => !IsSpecialityHidden(config, s.Id)).ToList() : specialities;

        public static List<DoctorItem> Filter(IConfiguration config, List<DoctorItem> doctors) =>
            Enabled(config) ? doctors.Where(d => !IsDoctorHidden(config, d)).ToList() : doctors;

        private static bool Contains(IConfiguration config, string key, string? id)
        {
            var value = id?.Trim();
            if (string.IsNullOrEmpty(value)) return false;
            return config.GetSection(key).GetChildren()
                .Any(c => string.Equals(c.Value?.Trim(), value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
