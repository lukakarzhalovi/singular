namespace VirtualRoulette.Models.DTOs;

public class BetHistoryDto
{
    public int Id { get; set; }
    public string BetString { get; set; }
    public decimal BetAmount { get; set; }
    public int WinningNumber { get; set; }
    public decimal WonAmount { get; set; }
    public Guid SpinId { get; set; }
    public DateTime CreatedAt { get; set; }
}