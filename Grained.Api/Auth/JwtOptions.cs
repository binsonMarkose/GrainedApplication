namespace Grained.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "grained.api";
    public string Audience { get; set; } = "grained.web";
    public int ExpiryMinutes { get; set; } = 480;
}
