using System.Security.Cryptography;
using ge.singular.roulette;
using Microsoft.AspNetCore.SignalR;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Hubs;
using VirtualRoulette.Models.DTOs;
using VirtualRoulette.Persistence;
using VirtualRoulette.Persistence.InMemoryCache;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Applications.Bet;

public interface IRouletteService
{
    Task<Result<BetResponse>> Bet(string betString, int userId, string ipAddress);
}

public class RouletteService(
    AppDbContext dbContext,
    IUserRepository userRepository,
    IBetRepository betRepository,
    IJackpotInMemoryCache jackpotInMemoryCache,
    IHubContext<JackpotHub> hubContext,
    ILogger<RouletteService> logger)
    : IRouletteService
{
    public async Task<Result<BetResponse>> Bet(string betString, int userId, string ipAddress)
    {
        var ibvr = CheckBets.IsValid(betString);
        if (!ibvr.getIsValid())
        {
            return Result.Failure<BetResponse>(DomainError.Bet.InvalidBet);
        }

        var betAmountInCents = ibvr.getBetAmount();

        var userResult = await userRepository.GetById(userId);
        if (userResult.IsFailure)
        {
            return Result.Failure<BetResponse>(userResult.Errors);
        }

        var user = userResult.Value;
        if (user == null)
        {
            return Result.Failure<BetResponse>(DomainError.User.NotFound);
        }

        var betAmountDecimal = betAmountInCents / 100m;
        if (user.Balance < betAmountDecimal)
        {
            return Result.Failure<BetResponse>(DomainError.Bet.InsufficientBalance);
        }

        //Begin database transaction
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        
        try
        {
            await dbContext.Entry(user).ReloadAsync();
            
            if (user.Balance < betAmountDecimal)
            {
                await transaction.RollbackAsync();
                return Result.Failure<BetResponse>(DomainError.Bet.InsufficientBalance);
            }

            user.Balance -= betAmountDecimal;

            var winningNumber = RandomNumberGenerator.GetInt32(0, 37);

            var wonAmountInCents = CheckBets.EstimateWin(betString, winningNumber);
            
            var currentJackpotResult = jackpotInMemoryCache.Get();
            if (currentJackpotResult.IsFailure)
            {
                //error
            }

            var currentJackpot = currentJackpotResult.Value;
            var contributionInInternalFormat = betAmountInCents * 100;
            
            var jackpotResult = jackpotInMemoryCache.Set(currentJackpot + contributionInInternalFormat);
            if (jackpotResult.IsFailure)
            {
                logger.LogError("Error jackpot increase with message {Errors}", 
                    jackpotResult.Errors.FirstOrDefault()?.Message);
            }

            await hubContext.Clients.Group("JackpotSubscribers")
                .SendAsync("JackpotUpdated", currentJackpotResult.Value);
            
            if (wonAmountInCents > 0)
            {
                var wonAmountDecimal = wonAmountInCents / 100m;
                user.Balance += wonAmountDecimal;
            }

            var bet = new Models.Entities.Bet
            {
                UserId = userId,
                BetString = betString,
                BetAmountInCents = betAmountInCents,
                WinningNumber = winningNumber,
                WonAmountInCents = wonAmountInCents,
                SpinId = Guid.NewGuid(),
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            var createBetResult = await betRepository.CreateAsync(bet);
            if (createBetResult.IsFailure)
            {
                await transaction.RollbackAsync();
                return Result.Failure<BetResponse>(createBetResult.Errors);
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = new BetResponse
            {
                Status = true,
                SpinId = bet.SpinId,
                WinningNumber = winningNumber,
                WonAmountInCents = wonAmountInCents
            };

            return Result.Success(response);
        }
        catch (Exception e)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception rollbackEx)
            {
                logger.LogError(rollbackEx, "Error during transaction rollback");
            }

            logger.LogError(e, "Error processing bet for user {UserId}", userId);
            return Result.Failure<BetResponse>(
                DomainError.DbError.Error(nameof(RouletteService), e.Message));
        }
    }
}