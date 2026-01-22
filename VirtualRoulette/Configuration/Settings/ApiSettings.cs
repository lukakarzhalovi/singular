namespace VirtualRoulette.Configuration.Settings;

public sealed record ApiSettings
{
    public string BasePath { get; set; } = string.Empty;
}
