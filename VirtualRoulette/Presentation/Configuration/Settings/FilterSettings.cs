namespace VirtualRoulette.Presentation.Configuration.Settings;

public sealed record FilterSettings
{
    public string UserIdKey { get; set; } = string.Empty;
    public string IpAddressKey { get; set; } = string.Empty;
}
