namespace Book_An_Appointment1.Helpers
{
    // Keeps personal data out of log files
    public static class LogMask
    {
        // 9876543210 → ******3210
        public static string Mobile(string? mobile)
        {
            var value = mobile?.Trim() ?? "";
            return value.Length <= 4 ? "****" : new string('*', value.Length - 4) + value[^4..];
        }
    }
}
