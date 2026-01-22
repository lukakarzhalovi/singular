namespace VirtualRoulette.Configuration.Settings;

public sealed record SignalRSettings
{
    public string HubPath { get; set; } = string.Empty;
    public string JackpotGroupName { get; set; } = string.Empty;
    public string JackpotUpdatedMethod { get; set; } = string.Empty;
    public string ForceDisconnectMethod { get; set; } = string.Empty;
}
