namespace VirtualRoulette.Presentation.Configuration.Settings;

public sealed record CorsSettings
{
    public string PolicyName { get; set; } = string.Empty;
    public List<string> AllowedOrigins { get; set; } = new();
}
