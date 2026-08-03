namespace Dsw2026Tpi.Api.Settings
{
    public class RateLimitingSettings
    {
        public int WindowMinutes { get; set; }
        public int AdminLoginLimit { get; set; }
        public int PatientLoginLimit { get; set; }
        public int BookingLimit { get; set; }
        public int GlobalLimit { get; set; }
    }
}
