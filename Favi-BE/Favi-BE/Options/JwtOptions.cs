namespace Favi_BE.Options
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public string Key { get; set; } = default!;
        public int AccessMinutes { get; set; } = 60;
        public int RefreshDays { get; set; } = 7;
    }
}
