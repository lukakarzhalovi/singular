using System.Security.Cryptography;
using ge.singular.roulette;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;
using VirtualRoulette.Configuration.Settings;
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
    ILogger<RouletteService> logger,
    IOptions<SignalRSettings> signalRSettings,
    IOptions<JackpotSettings> jackpotSettings)
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

        if (user.Balance < betAmountInCents)
        {
            return Result.Failure<BetResponse>(DomainError.Bet.InsufficientBalance);
        }

        await unitOfWork.BeginTransactionAsync();
        
        try
        {
            if (user.Balance < betAmountInCents)
            {
                await unitOfWork.RollbackTransactionAsync();
                return Result.Failure<BetResponse>(DomainError.Bet.InsufficientBalance);
            }

            user.Balance -= betAmountInCents;

            var winningNumber = RandomNumberGenerator.GetInt32(0, 37);

            var wonAmountInCents = CheckBets.EstimateWin(betString, winningNumber);
            
            if (wonAmountInCents > 0)
            {
                user.Balance += wonAmountInCents;
                
                var currentJackpotResult = jackpotInMemoryCache.Get();
                if (currentJackpotResult.IsFailure)
                {
                    logger.LogError("Error getting jackpot with message {Errors}", 
                        currentJackpotResult.Errors.FirstOrDefault()?.Message);
                    // Continue with bet even if jackpot get fails - use 0 as fallback
                }

                var currentJackpot = currentJackpotResult.IsSuccess ? currentJackpotResult.Value : 0;
            
                var signalRSettingsValue = signalRSettings.Value;
                var jackpotSettingsValue = jackpotSettings.Value;
                var contributionPercentage = jackpotSettingsValue.ContributionPercentage;
                var contributionInCents = betAmountInCents * contributionPercentage;
                var contributionInInternalFormat = (long)(contributionInCents * 10000);
                
                var newJackpotValue = currentJackpot + contributionInInternalFormat;
                var setJackpotResult = jackpotInMemoryCache.Set(newJackpotValue);
                if (setJackpotResult.IsFailure)
                {
                    logger.LogError("Error setting jackpot with message {Errors}", 
                        setJackpotResult.Errors.FirstOrDefault()?.Message);
                    // Continue with bet even if jackpot set fails
                }
                else
                {
                    // Send updated jackpot value to all connected clients
                    await hubContext.Clients.Group(signalRSettingsValue.JackpotGroupName)
                        .SendAsync(signalRSettingsValue.JackpotUpdatedMethod, newJackpotValue);
                }
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