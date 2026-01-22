namespace VirtualRoulette.Presentation.Configuration.Settings;

public sealed record DatabaseSettings
{
    public string DatabaseName { get; set; } = string.Empty;
}
