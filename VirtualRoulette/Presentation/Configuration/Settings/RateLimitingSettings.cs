namespace VirtualRoulette.Presentation.Configuration.Settings;

public sealed record RateLimitingSettings
{
    public string PolicyName { get; set; } = string.Empty;
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
    public int QueueLimit { get; set; }
}
