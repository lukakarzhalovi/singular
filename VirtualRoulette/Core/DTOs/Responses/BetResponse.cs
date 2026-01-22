namespace VirtualRoulette.Core.DTOs.Responses;

public sealed record BetResponse
{
    public bool Status { get; set; }
   
    public Guid SpinId { get; set; }
   
    public int WinningNumber { get; set; }
    
    public long WonAmountInCents { get; set; }
}
