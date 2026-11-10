namespace Book_An_Appointment1.Helpers
{
    // Fees shown to patients are whole rupees (cash at the counter): 1.50 → 2, 900.00 → 900.
    // Same rule on every page (doctor card, patient details, review, popup, slip, doctor profile).
    // The total is the sum of the rounded lines, so the lines on the Review page always add up.
    public static class Fee
    {
        public static decimal Round(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);

        public static string Text(decimal value) => Round(value).ToString("N0");

        public static decimal Total(params decimal[] parts) => parts.Sum(Round);
    }
}
