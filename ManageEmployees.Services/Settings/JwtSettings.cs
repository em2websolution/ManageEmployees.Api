namespace ManageEmployees.Services.Settings
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string ExpiresAt { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
    }
}
