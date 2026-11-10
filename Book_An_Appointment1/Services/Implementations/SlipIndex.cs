using Book_An_Appointment1.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Book_An_Appointment1.Services.Implementations
{
    // "Find my slip": small file store of issued slips — App_Data/slips/{appointmentId}.json
    // (or FindSlipSettings:StorePath). The hospital API cannot list a patient's appointments by mobile,
    // so the portal remembers the slips it issued. Records older than SlipSettings:LinkValidDays are deleted.
    // Mobile numbers are stored only as an HMAC with a random per-server secret (App_Data/slips/.secret).
    // Several servers behind a load balancer: point StorePath of all of them to one shared folder.
    public class SlipIndex : ISlipIndex
    {
        private static readonly object Gate = new();
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

        private readonly IConfiguration _configuration;
        private readonly ILogger<SlipIndex> _logger;
        private readonly string _folder;
        private DateTime _lastCleanup = DateTime.MinValue;
        private byte[]? _secret;

        public SlipIndex(IConfiguration configuration, IWebHostEnvironment environment, ILogger<SlipIndex> logger)
        {
            _configuration = configuration;
            _logger = logger;
            var path = configuration["FindSlipSettings:StorePath"];
            _folder = string.IsNullOrWhiteSpace(path) ? Path.Combine(environment.ContentRootPath, "App_Data", "slips") : path;
        }

        private int KeepDays => Math.Clamp(_configuration.GetValue("SlipSettings:LinkValidDays", 30), 1, 365);

        public void Save(string mobile, SlipRecord record)
        {
            if (string.IsNullOrWhiteSpace(mobile) || !long.TryParse(record.AppointmentId, out _)) return;
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(_folder);
                    record.MobileHash = Hash(mobile);
                    var m = mobile.Trim();
                    record.MaskedMobile = m.Length > 4 ? new string('X', m.Length - 4) + m[^4..] : m;   // never the full number
                    record.CreatedUtc = DateTime.UtcNow;
                    File.WriteAllText(Path.Combine(_folder, record.AppointmentId + ".json"), JsonSerializer.Serialize(record, Json));
                    CleanupIfDue();
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Never breaks the booking — only "Find my slip" will not know this one
                _logger.LogWarning(ex, "Find my slip: could not save slip record | AppointmentId={A}", record.AppointmentId);
            }
        }

        public IReadOnlyList<SlipRecord> FindByMobile(string mobile)
        {
            var result = new List<SlipRecord>();
            if (string.IsNullOrWhiteSpace(mobile) || !Directory.Exists(_folder)) return result;
            try
            {
                var hash = Hash(mobile);
                var oldest = DateTime.UtcNow.AddDays(-KeepDays);
                foreach (var file in Directory.EnumerateFiles(_folder, "*.json"))
                {
                    var record = JsonSerializer.Deserialize<SlipRecord>(File.ReadAllText(file));
                    if (record != null && record.CreatedUtc >= oldest &&
                        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(record.MobileHash), Encoding.ASCII.GetBytes(hash)))
                        result.Add(record);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogWarning(ex, "Find my slip: could not read slip records");
            }
            return result.OrderByDescending(r => r.StartsAt ?? DateTime.MinValue).Take(20).ToList();
        }

        // HMAC-SHA256 of the mobile with a random secret kept on this server (created once)
        private string Hash(string mobile)
        {
            _secret ??= LoadSecret();
            using var hmac = new HMACSHA256(_secret);
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(mobile.Trim())));
        }

        private byte[] LoadSecret()
        {
            lock (Gate)
            {
                Directory.CreateDirectory(_folder);
                var file = Path.Combine(_folder, ".secret");
                if (File.Exists(file)) return Convert.FromBase64String(File.ReadAllText(file).Trim());
                var secret = RandomNumberGenerator.GetBytes(32);
                File.WriteAllText(file, Convert.ToBase64String(secret));
                return secret;
            }
        }

        // At most once an hour: delete records older than the slip link lifetime
        private void CleanupIfDue()
        {
            if (DateTime.UtcNow - _lastCleanup < TimeSpan.FromHours(1)) return;
            _lastCleanup = DateTime.UtcNow;
            var oldest = DateTime.UtcNow.AddDays(-KeepDays);
            foreach (var file in Directory.EnumerateFiles(_folder, "*.json"))
                if (File.GetLastWriteTimeUtc(file) < oldest)
                    try { File.Delete(file); } catch (IOException) { }
        }
    }
}
