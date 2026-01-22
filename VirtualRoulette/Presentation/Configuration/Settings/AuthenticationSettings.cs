namespace VirtualRoulette.Configuration.Settings;

public sealed record AuthenticationSettings
{
    public CookieSettings Cookie { get; set; } = new();
    public string LoginPath { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
    public bool SlidingExpiration { get; set; }
    public int SessionIdleTimeoutMinutes { get; set; }
}

public sealed record CookieSettings
{
    public bool HttpOnly { get; set; }
    public string SecurePolicy { get; set; } = string.Empty;
    public string SameSite { get; set; } = string.Empty;
    public bool IsEssential { get; set; }
}
