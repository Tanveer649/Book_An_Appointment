using Book_An_Appointment1.API.Clients;
using Book_An_Appointment1.API.EndPoints;
using Book_An_Appointment1.Models.Patient;
using Book_An_Appointment1.Services.Interfaces;
using BookAppointmentPortal.Api.Clients;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Book_An_Appointment1.Services.Implementations
{
    public class PatientRegistrationService : BaseApiClient, IPatientRegistrationService
    {
        public PatientRegistrationService(IHttpClientFactory clientFactory, ILogger<PatientRegistrationService> logger, ApiUrlBuilder urlBuilder)
            : base(clientFactory, logger, urlBuilder)
        {
        }

        public async Task<FamilyMemberRegistrationResult> SaveFamilyMemberAsync(FamilyMemberRegistrationRequest request)
        {
            // Outgoing body at Debug (OTP masked) — compare with the SQL profiler when a value does not arrive
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                var body = JObject.FromObject(request);
                body["OTP"] = "******";
                Logger.LogDebug("MobSaveRegistrationForPatient request body: {Body}", body.ToString(Newtonsoft.Json.Formatting.None));
            }

            var json = await PostAsync<JObject>(ApiRoutes.SaveFamilyMember, request);
            if (json == null)
                return new FamilyMemberRegistrationResult { Message = "" };

            var success = string.Equals(Str(json, "Status"), "success", StringComparison.OrdinalIgnoreCase);
            var result = new FamilyMemberRegistrationResult
            {
                Success = success,
                Message = Str(json, "ErrorMessage") ?? ""
            };
            if (!success) return result;

            // The response repeats many fields in two spellings, e.g. "firstName": "Subarna" and "FirstName": null.
            // A normal (case-insensitive) mapping would let the null overwrite the value, so each field is read
            // by its exact name — the lower-case ones carry the new member's data.
            result.Patient = new RegisteredPatientInfo
            {
                HospitalLocationId = Str(json, "hospitalLocationId"),
                FacilityID = Str(json, "facilityID"),
                RegistrationId = int.TryParse(Str(json, "registrationId"), out var regId) ? regId : 0,
                RegistrationNo = Str(json, "registrationNo") ?? "0",
                GuestPatientId = Str(json, "GuestPateintId") ?? Str(json, "RegId"),
                EncounterId = Str(json, "encounterId"),
                EncounterNo = Str(json, "encounterNo"),
                PatientName = Str(json, "patientName"),
                TitleId = Str(json, "titleId"),
                TitleName = Str(json, "titleName"),
                FirstName = Str(json, "firstName"),
                MiddleName = Str(json, "middleName"),
                LastName = Str(json, "lastname") ?? Str(json, "lastName"),
                Gender = NormalizeGender(Str(json, "gender")),
                Dob = NormalizeDate(Str(json, "dob")),
                Age = Str(json, "age"),
                EmailId = Str(json, "emailId"),
                MobileNo = Str(json, "mobileNo"),
                PatientType = Str(json, "patientType"),
                IsUnRegPat = Str(json, "isUnRegPat") is "1" or "true" or "True",
                TcFacilityId = Str(json, "tcFacilityId")
            };
            return result;
        }

        // Exact-name lookup; empty / whitespace values count as missing
        private static string? Str(JObject json, string name)
        {
            var value = json.Property(name, StringComparison.Ordinal)?.Value;
            if (value == null || value.Type == JTokenType.Null) return null;
            var text = value.ToString().Trim();
            return text.Length == 0 ? null : text;
        }

        // "FeMale" → "Female" (the cards translate Male / Female / Other)
        private static string? NormalizeGender(string? gender) => gender?.ToLowerInvariant() switch
        {
            "male" or "m" => "Male",
            "female" or "f" => "Female",
            "other" or "o" => "Other",
            _ => gender
        };

        // "2/20/2001 12:00:00 AM" → "2001-02-20"
        private static string? NormalizeDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : value;
    }
}
