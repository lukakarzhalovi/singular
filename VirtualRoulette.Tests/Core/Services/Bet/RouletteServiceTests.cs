using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using VirtualRoulette.Core.DTOs.Responses;
using VirtualRoulette.Core.Entities;
using VirtualRoulette.Core.Services.Bet;
using VirtualRoulette.Infrastructure.Persistence.Caching;
using VirtualRoulette.Infrastructure.Persistence.Repositories;
using VirtualRoulette.Infrastructure.SignalR.Hubs;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Result;
using Xunit;
using Entities = VirtualRoulette.Core.Entities;

namespace VirtualRoulette.Tests.Core.Services.Bet;

public class RouletteServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IBetRepository _betRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJackpotInMemoryCache _jackpotCache;
    private readonly IHubContext<JackpotHub> _hubContext;
    private readonly ILogger<RouletteService> _logger;
    private readonly IOptions<SignalRSettings> _signalRSettings;
    private readonly IOptions<JackpotSettings> _jackpotSettings;
    private readonly RouletteService _rouletteService;

    public RouletteServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _betRepository = Substitute.For<IBetRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _jackpotCache = Substitute.For<IJackpotInMemoryCache>();
        _hubContext = Substitute.For<IHubContext<JackpotHub>>();
        _logger = Substitute.For<ILogger<RouletteService>>();
        _signalRSettings = Options.Create(new SignalRSettings
        {
            HubPath = "/hub",
            JackpotGroupName = "jackpot",
            JackpotUpdatedMethod = "JackpotUpdated",
            ForceDisconnectMethod = "ForceDisconnect"
        });
        _jackpotSettings = Options.Create(new JackpotSettings
        {
            ContributionPercentage = 0.01m
        });

        _rouletteService = new RouletteService(
            _userRepository,
            _betRepository,
            _unitOfWork,
            _jackpotCache,
            _hubContext,
            _logger,
            _signalRSettings,
            _jackpotSettings
        );
    }

    [Fact]
    public async Task Bet_WithInvalidBetString_ShouldReturnFailure()
    {
        // Arrange
        var betString = "invalid_bet_string";
        var userId = 1;
        var ipAddress = "127.0.0.1";

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.Bet.InvalidBet);
        await _userRepository.DidNotReceive().GetById(Arg.Any<int>());
    }

    [Fact]
    public async Task Bet_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var betString = "[{\"T\": \"v\", \"I\": 20, \"C\": 1, \"K\": 1}]";
        var userId = 1;
        var ipAddress = "127.0.0.1";
        _userRepository
            .GetById(userId)
            .Returns(Result.Success<Entities.User?>(null));

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.User.NotFound);
    }

    [Fact]
    public async Task Bet_WhenInsufficientBalance_ShouldReturnFailure()
    {
        // Arrange
        var betString = "[{\"T\": \"v\", \"I\": 20, \"C\": 10, \"K\": 1}]"; 
        var userId = 1;
        var ipAddress = "127.0.0.1";
        var user = CreateUser(userId, balance: 0); 
        _userRepository
            .GetById(userId)
            .Returns(Result.Success<Entities.User?>(user));

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.Bet.InsufficientBalance);
    }

    [Fact]
    public async Task Bet_WithValidBet_ShouldProcessBetSuccessfully()
    {
        // Arrange
        var betString = "[{\"T\": \"v\", \"I\": 20, \"C\": 1, \"K\": 1}]";
        var userId = 1;
        var ipAddress = "127.0.0.1";
        var user = CreateUser(userId, balance: 1000);
        SetupSuccessfulBetScenario(userId, user, betString);

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().BeTrue();
        result.Value.SpinId.Should().NotBeEmpty();
        result.Value.WinningNumber.Should().BeInRange(0, 36);
        await _betRepository.Received(1).CreateAsync(Arg.Any<Entities.Bet>());
        await _unitOfWork.Received(1).CommitTransactionAsync();
    }

    [Fact]
    public async Task Bet_WhenCreateBetFails_ShouldRollbackTransaction()
    {
        // Arrange
        var betString = "[{\"T\": \"v\", \"I\": 20, \"C\": 1, \"K\": 1}]";
        var userId = 1;
        var ipAddress = "127.0.0.1";
        var user = CreateUser(userId, balance: 1000);
        var error = DomainError.Bet.NotFound;
        SetupBetScenarioWithCreateFailure(userId, user, betString, error);

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
        await _unitOfWork.Received(1).RollbackTransactionAsync();
        await _unitOfWork.DidNotReceive().CommitTransactionAsync();
    }

    [Fact]
    public async Task Bet_WhenSaveChangesFails_ShouldRollbackTransaction()
    {
        // Arrange
        var betString = "[{\"T\": \"v\", \"I\": 20, \"C\": 1, \"K\": 1}]";
        var userId = 1;
        var ipAddress = "127.0.0.1";
        var user = CreateUser(userId, balance: 1000);
        var error = DomainError.DbError.Error(nameof(RouletteService), "Save failed");
        SetupBetScenarioWithSaveFailure(userId, user, betString, error);

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
        await _unitOfWork.Received(1).RollbackTransactionAsync();
        await _unitOfWork.DidNotReceive().CommitTransactionAsync();
    }

    [Fact]
    public async Task Bet_ShouldDeductBetAmountFromBalance()
    {
        // Arrange
        var betString = "[{\"T\": \"v\", \"I\": 20, \"C\": 1, \"K\": 1}]";
        var userId = 1;
        var ipAddress = "127.0.0.1";
        var initialBalance = 1000L;
        var user = CreateUser(userId, balance: initialBalance);
        SetupSuccessfulBetScenario(userId, user, betString);

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Balance.Should().BeLessThan(initialBalance);
    }

    [Fact]
    public async Task Bet_ShouldUpdateJackpot()
    {
        // Arrange
        var betString = "[{\"T\": \"v\", \"I\": 20, \"C\": 1, \"K\": 1}]";
        var userId = 1;
        var ipAddress = "127.0.0.1";
        var user = CreateUser(userId, balance: 1000);
        var currentJackpot = 50000L;
        _jackpotCache
            .Get()
            .Returns(Result.Success(currentJackpot));
        _jackpotCache
            .Set(Arg.Any<long>())
            .Returns(Result.Success());
        SetupSuccessfulBetScenario(userId, user, betString, currentJackpot);

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _jackpotCache.Received(1).Get();
        _jackpotCache.Received(1).Set(Arg.Any<long>());
    }

    [Fact]
    public async Task Bet_WhenUserNotFoundInTransaction_ShouldRollback()
    {
        // Arrange
        var betString = "[{\"T\": \"v\", \"I\": 20, \"C\": 1, \"K\": 1}]";
        var userId = 1;
        var ipAddress = "127.0.0.1";
        var user = CreateUser(userId, balance: 1000);
        _userRepository
            .GetById(userId)
            .Returns(
                Result.Success<Entities.User?>(user),
                Result.Success<Entities.User?>(null)
            );
        _unitOfWork.BeginTransactionAsync().Returns(Task.CompletedTask);

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.User.NotFound);
        await _unitOfWork.Received(1).RollbackTransactionAsync();
    }

    [Fact]
    public async Task Bet_WhenInsufficientBalanceInTransaction_ShouldRollback()
    {
        // Arrange
        var betString = "[{\"T\": \"v\", \"I\": 20, \"C\": 1, \"K\": 1}]";
        var userId = 1;
        var ipAddress = "127.0.0.1";
        var userWithBalance = CreateUser(userId, balance: 1000);
        var userWithoutBalance = CreateUser(userId, balance: 0);
        _userRepository
            .GetById(userId)
            .Returns(
                Result.Success<Entities.User?>(userWithBalance),
                Result.Success<Entities.User?>(userWithoutBalance)
            );
        _unitOfWork.BeginTransactionAsync().Returns(Task.CompletedTask);

        // Act
        var result = await _rouletteService.Bet(betString, userId, ipAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.Bet.InsufficientBalance);
        await _unitOfWork.Received(1).RollbackTransactionAsync();
    }

    private void SetupSuccessfulBetScenario(int userId, Entities.User user, string betString, long initialJackpot = 0L)
    {
        _userRepository
            .GetById(userId)
            .Returns(
                Result.Success<Entities.User?>(user),
                Result.Success<Entities.User?>(user)
            );
        _jackpotCache
            .Get()
            .Returns(Result.Success(initialJackpot));
        _jackpotCache
            .Set(Arg.Any<long>())
            .Returns(Result.Success());
        _betRepository
            .CreateAsync(Arg.Any<Entities.Bet>())
            .Returns(Result.Success(new Entities.Bet
            {
                BetString = betString,
                IpAddress = "127.0.0.1"
            }));
        _unitOfWork
            .BeginTransactionAsync()
            .Returns(Task.CompletedTask);
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _unitOfWork
            .CommitTransactionAsync()
            .Returns(Task.CompletedTask);
    }

    private void SetupBetScenarioWithCreateFailure(int userId, Entities.User user, string betString, Error error)
    {
        _userRepository
            .GetById(userId)
            .Returns(
                Result.Success<Entities.User?>(user),
                Result.Success<Entities.User?>(user)
            );
        _jackpotCache
            .Get()
            .Returns(Result.Success(0L));
        _jackpotCache
            .Set(Arg.Any<long>())
            .Returns(Result.Success());
        _betRepository
            .CreateAsync(Arg.Any<Entities.Bet>())
            .Returns(Result.Failure<Entities.Bet>(error));
        _unitOfWork
            .BeginTransactionAsync()
            .Returns(Task.CompletedTask);
        _unitOfWork
            .RollbackTransactionAsync()
            .Returns(Task.CompletedTask);
    }

    private void SetupBetScenarioWithSaveFailure(int userId, Entities.User user, string betString, Error error)
    {
        _userRepository
            .GetById(userId)
            .Returns(
                Result.Success<Entities.User?>(user),
                Result.Success<Entities.User?>(user)
            );
        _jackpotCache
            .Get()
            .Returns(Result.Success(0L));
        _jackpotCache
            .Set(Arg.Any<long>())
            .Returns(Result.Success());
        _betRepository
            .CreateAsync(Arg.Any<Entities.Bet>())
            .Returns(Result.Success(new Entities.Bet
            {
                BetString = betString,
                IpAddress = "127.0.0.1"
            }));
        _unitOfWork
            .BeginTransactionAsync()
            .Returns(Task.CompletedTask);
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));
        _unitOfWork
            .RollbackTransactionAsync()
            .Returns(Task.CompletedTask);
    }

    private static Entities.User CreateUser(int id, long balance)
    {
        return new Entities.User
        {
            Id = id,
            Username = "testuser",
            PasswordHash = "hash",
            Balance = balance,
            CreatedAt = DateTime.UtcNow
        };
    }
}
