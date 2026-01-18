namespace VirtualRoulette.Models.DTOs;

/// <summary>
/// Request model for increasing jackpot (for testing purposes).
/// </summary>
public class IncreaseJackpotRequest
{
    /// <summary>
    /// Amount to add in cents. Will be converted to internal format (1 cent = 10,000).
    /// </summary>
    public int AmountInCents { get; set; }
}
