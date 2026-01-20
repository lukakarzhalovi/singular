using System.Security.Cryptography;
using ge.singular.roulette;
using Microsoft.AspNetCore.SignalR;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Hubs;
using VirtualRoulette.Models.DTOs;
using VirtualRoulette.Persistence.InMemoryCache;
using VirtualRoulette.Persistence.Repositories;

namespace VirtualRoulette.Applications.Bet;

public interface IRouletteService
{
    Task<Result<BetResponse>> Bet(string betString, int userId, string ipAddress);
}

public class RouletteService(
    IUserRepository userRepository,
    IBetRepository betRepository,
    IUnitOfWork unitOfWork,
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
        await unitOfWork.BeginTransactionAsync();
        
        try
        {
            if (user.Balance < betAmountDecimal)
            {
                await unitOfWork.RollbackTransactionAsync();
                return Result.Failure<BetResponse>(DomainError.Bet.InsufficientBalance);
            }

            user.Balance -= betAmountDecimal;

            var winningNumber = RandomNumberGenerator.GetInt32(0, 37);

            var wonAmountInCents = CheckBets.EstimateWin(betString, winningNumber);
            
            var currentJackpotResult = jackpotInMemoryCache.Get();
            if (currentJackpotResult.IsFailure)
            {
                logger.LogError("Error jackpot get with message {Errors}", 
                    currentJackpotResult.Errors.FirstOrDefault()?.Message);            
            }

            var currentJackpot = currentJackpotResult.Value;
            var contributionInInternalFormat = betAmountInCents * 100;
            
            var setJackpotResult = jackpotInMemoryCache.Set(currentJackpot + contributionInInternalFormat);
            if (setJackpotResult.IsFailure)
            {
                logger.LogError("Error jackpot increase with message {Errors}", 
                    setJackpotResult.Errors.FirstOrDefault()?.Message);
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
                await unitOfWork.RollbackTransactionAsync();
                return Result.Failure<BetResponse>(createBetResult.Errors);
            }

            var saveResult = await unitOfWork.SaveChangesAsync();
            if (saveResult.IsFailure)
            {
                await unitOfWork.RollbackTransactionAsync();
                return Result.Failure<BetResponse>(saveResult.Errors);
            }

            await unitOfWork.CommitTransactionAsync();

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
                await unitOfWork.RollbackTransactionAsync();
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