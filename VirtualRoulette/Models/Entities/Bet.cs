namespace VirtualRoulette.Models.Entities;

public class Bet
{
    public int Id { get; init; }
    
    public int UserId { get; set; }
    
    public User User { get; set; }
   
    public required string BetString { get; set; }
  
    public long BetAmountInCents { get; set; }
    
    public int WinningNumber { get; set; }
    
    public long WonAmountInCents { get; set; }
    
    public Guid SpinId { get; set; }
    
    public required string IpAddress { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
