namespace VirtualRoulette.Core.Entities;

public class User
{
    public int Id { get; init; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; } 
    public required DateTime CreatedAt { get; set; } 
    public required long Balance { get; set; }
}
