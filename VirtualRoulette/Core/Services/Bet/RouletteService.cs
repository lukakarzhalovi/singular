using System.Security.Cryptography;
using ge.singular.roulette;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using VirtualRoulette.Shared;
using VirtualRoulette.Infrastructure.SignalR.Hubs;
using VirtualRoulette.Core.DTOs.Responses;
using VirtualRoulette.Infrastructure.Persistence.Caching;
using VirtualRoulette.Infrastructure.Persistence.Repositories;
using VirtualRoulette.Core.Entities;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Result;
using BetEntity = VirtualRoulette.Core.Entities.Bet;

namespace VirtualRoulette.Core.Services.Bet;

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
        // Check if the bet string is valid using the roulette library
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

        // Early balance check to avoid starting unnecessary transactions
        // Note: This is just an optimization - the real check happens inside the transaction
        if (user.Balance < betAmountInCents)
        {
            return Result.Failure<BetResponse>(DomainError.Bet.InsufficientBalance);
        }

        await unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Reload user inside transaction to get fresh balance data
            // This prevents race conditions where another bet could have reduced balance
            // between the first check and transaction start
            // In production: Consider using database-level constraints or optimistic concurrency
            // (e.g., RowVersion/Timestamp) for better performance and reliability
            var freshUserResult = await userRepository.GetById(userId);
            if (freshUserResult.IsFailure || freshUserResult.Value == null)
            {
                await unitOfWork.RollbackTransactionAsync();
                return Result.Failure<BetResponse>(DomainError.User.NotFound);
            }
            
            var freshUser = freshUserResult.Value;
            if (freshUser.Balance < betAmountInCents)
            {
                await unitOfWork.RollbackTransactionAsync();
                return Result.Failure<BetResponse>(DomainError.Bet.InsufficientBalance);
            }
            
            // Deduct bet amount from user balance
            freshUser.Balance -= betAmountInCents;

            // Generate random winning number (0-36)
            var winningNumber = RandomNumberGenerator.GetInt32(0, 37);

            // Calculate winnings using the roulette library
            var wonAmountInCents = CheckBets.EstimateWin(betString, winningNumber);
            
            // Add winnings to balance if user won
            if (wonAmountInCents > 0)
            {
                freshUser.Balance += wonAmountInCents;
            }
            
            // Update jackpot - 1% of every bet goes to jackpot
            var currentJackpotResult = jackpotInMemoryCache.Get();
            if (currentJackpotResult.IsFailure)
            {
                logger.LogError("Error getting jackpot with message {Errors}", 
                    currentJackpotResult.Errors.FirstOrDefault()?.Message);
            }

            var currentJackpot = currentJackpotResult.IsSuccess ? currentJackpotResult.Value : 0;
        
            var signalRSettingsValue = signalRSettings.Value;
            var jackpotSettingsValue = jackpotSettings.Value;
            var contributionPercentage = jackpotSettingsValue.ContributionPercentage;
            // Calculate contribution: bet amount * 1% * 10000 (internal format)
            var contributionInCents = betAmountInCents * contributionPercentage;
            var contributionInInternalFormat = (long)(contributionInCents * 10000);
            
            var newJackpotValue = currentJackpot + contributionInInternalFormat;
            var setJackpotResult = jackpotInMemoryCache.Set(newJackpotValue);
            if (setJackpotResult.IsFailure)
            {
                logger.LogError("Error setting jackpot with message {Errors}", 
                    setJackpotResult.Errors.FirstOrDefault()?.Message);
            }
            else
            {
                // Notify all connected clients about jackpot update
                await hubContext.Clients.Group(signalRSettingsValue.JackpotGroupName)
                    .SendAsync(signalRSettingsValue.JackpotUpdatedMethod, newJackpotValue);
            }
            
            // Save bet record with IP address and timestamp
            var bet = new BetEntity
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

            // Save all changes to database
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