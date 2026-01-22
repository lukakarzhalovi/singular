namespace VirtualRoulette.Core.DTOs.Requests;

public sealed record SignInRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
