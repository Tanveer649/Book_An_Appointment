using Book_An_Appointment1.Helpers;
using Book_An_Appointment1.Models.DoctorItem;
using Book_An_Appointment1.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace Book_An_Appointment1.Services.Implementations
{
    // Looks a doctor up by id for public links (/doctor/{id}, /book/doctor/{id}).
    // Results are cached for a few minutes: these pages are opened from the hospital website,
    // so many visitors hit the same doctors and the hospital API is not called each time.
    public class DoctorDirectory : IDoctorDirectory
    {
        private static readonly TimeSpan FoundFor = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan MissingFor = TimeSpan.FromMinutes(2);

        private readonly IDoctorService _doctorService;
        private readonly IFacilityService _facilityService;
        private readonly IConsultationService _consultationService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DoctorDirectory> _logger;

        public DoctorDirectory(IDoctorService doctorService, IFacilityService facilityService,
            IConsultationService consultationService, IMemoryCache cache, IConfiguration configuration, ILogger<DoctorDirectory> logger)
        {
            _doctorService = doctorService;
            _facilityService = facilityService;
            _consultationService = consultationService;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        // appsettings HideList: a hidden doctor (or a doctor of a hidden speciality) is "not found" for links and profiles.
        // Checked after the cache, so a change in appsettings.json applies at once.
        public async Task<DoctorMatch?> FindAsync(int doctorId, int? facilityId = null)
        {
            var match = await FindAnyAsync(doctorId, facilityId);
            if (match != null && HideList.IsDoctorHidden(_configuration, match.Doctor))
            {
                _logger.LogInformation("Doctor link — doctor is on the hide list | DoctorId={D}", doctorId);
                return null;
            }
            return match;
        }

        private async Task<DoctorMatch?> FindAnyAsync(int doctorId, int? facilityId)
        {
            if (doctorId <= 0) return null;

            var key = $"doctor:{facilityId ?? 0}:{doctorId}";
            if (_cache.TryGetValue(key, out DoctorMatch? cached)) return cached;

            DoctorMatch? match = null;
            try
            {
                // Hidden facilities (appsettings HideList) are never searched → their doctors are "not found" for links / profiles
                var facilities = HideList.Filter(_configuration, (await _facilityService.GetFacilitiesAsync())?.Data ?? new());

                // A facility id that is not one of this portal's facilities is ignored (searched everywhere instead)
                var search = facilityId is > 0 && facilities.Any(f => f.FacilityId == facilityId)
                    ? facilities.Where(f => f.FacilityId == facilityId).ToList()
                    : facilities;

                foreach (var facility in search)
                {
                    var doctor = (await _doctorService.GetDoctorAsync(facility.FacilityId, doctorId))?.Data?
                        .FirstOrDefault(d => d.DoctorId == doctorId);
                    if (doctor == null) continue;

                    int.TryParse(doctor.SpecialisationId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var specialityId);
                    match = new DoctorMatch(doctor, facility.FacilityId, specialityId, facility.FacilityName ?? "");
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doctor lookup failed | DoctorId={D} FacilityId={F}", doctorId, facilityId);
                return null;   // API down — not cached, next visitor tries again
            }

            if (match == null)
                _logger.LogWarning("Doctor link — doctor not found | DoctorId={D} FacilityId={F}", doctorId, facilityId);

            _cache.Set(key, match, match != null ? FoundFor : MissingFor);
            return match;
        }

        public async Task<decimal?> ConsultationFeeAsync(int facilityId, DoctorItem doctor)
        {
            var key = $"doctor-fee:{facilityId}:{doctor.DoctorId}";
            if (_cache.TryGetValue(key, out decimal? cached)) return cached;

            decimal? fee = null;
            try
            {
                var item = (await _consultationService.GetConsultationFeeAsync(facilityId, doctor.DoctorId))?
                    .FirstOrDefault(x => x.ServiceType == "CL");
                fee = ParseMoney(item?.NetAmount) ?? ParseMoney(item?.Price);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Consultation fee lookup failed | DoctorId={D}", doctor.DoctorId);
            }

            fee ??= ParseMoney(doctor.DoctorFee);
            _cache.Set(key, fee, FoundFor);
            return fee;
        }

        private static decimal? ParseMoney(string? raw) =>
            decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d > 0 ? d : null;
    }
}
