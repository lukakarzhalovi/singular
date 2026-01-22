namespace VirtualRoulette.Configuration.Settings;

public sealed record ActivityTrackingSettings
{
    public int InactivityTimeoutMinutes { get; set; }
    public string CacheKeyPrefix { get; set; } = string.Empty;
}
