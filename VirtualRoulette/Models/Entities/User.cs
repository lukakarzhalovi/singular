namespace VirtualRoulette.Models.Entities;

public class User
{
    public int Id { get; init; }
    public required string Username { get; init; }
    public required string PasswordHash { get; init; } 
    public required DateTime CreatedAt { get; init; } 
    public required int Balance { get; init; }
}
