using FluentAssertions;
using NSubstitute;
using VirtualRoulette.Core.Services.ActivityTracker;
using VirtualRoulette.Core.Services.User;
using VirtualRoulette.Infrastructure.Persistence.Repositories;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Pagination;
using VirtualRoulette.Shared.Result;
using Xunit;
using UserEntity = VirtualRoulette.Core.Entities.User;
using BetEntity = VirtualRoulette.Core.Entities.Bet;

namespace VirtualRoulette.Tests.Core.Services.User;

public class UserServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IBetRepository _betRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserActivityTracker _activityTracker;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _betRepository = Substitute.For<IBetRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _activityTracker = Substitute.For<IUserActivityTracker>();

        _userService = new UserService(
            _userRepository,
            _betRepository,
            _unitOfWork,
            _activityTracker
        );
    }

    [Fact]
    public async Task GetBalance_WithValidUserId_ShouldReturnBalance()
    {
        // Arrange
        var userId = 1;
        var user = CreateUser(userId, balance: 10000);
        _userRepository
            .GetById(userId)
            .Returns(Result.Success<UserEntity?>(user));

        // Act
        var result = await _userService.GetBalance(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10000);
        await _userRepository.Received(1).GetById(userId);
    }

    [Fact]
    public async Task GetBalance_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        _userRepository
            .GetById(userId)
            .Returns(Result.Success<UserEntity?>(null));

        // Act
        var result = await _userService.GetBalance(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.User.NotFound);
    }

    [Fact]
    public async Task GetBalance_WhenRepositoryFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        var error = DomainError.User.NotFound;
        _userRepository
            .GetById(userId)
            .Returns(Result.Failure<UserEntity?>(error));

        // Act
        var result = await _userService.GetBalance(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
    }

    [Fact]
    public async Task GetBets_WithValidUserId_ShouldReturnPagedBets()
    {
        // Arrange
        var userId = 1;
        var page = 1;
        var limit = 10;
        var bets = CreateBets(userId, 5);
        var pagedList = new PagedList<BetEntity>
        {
            Items = bets,
            TotalCount = 5
        };

        _betRepository
            .GetByUserId(userId, Arg.Any<int>(), limit)
            .Returns(Result.Success(pagedList));

        // Act
        var result = await _userService.GetBets(userId, page, limit);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        await _betRepository.Received(1).GetByUserId(userId, 0, limit);
    }

    [Fact]
    public async Task GetBets_WhenRepositoryFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        var page = 1;
        var limit = 10;
        var error = DomainError.Bet.NotFound;
        _betRepository
            .GetByUserId(userId, Arg.Any<int>(), limit)
            .Returns(Result.Failure<PagedList<BetEntity>>(error));

        // Act
        var result = await _userService.GetBets(userId, page, limit);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
    }

    [Fact]
    public async Task AddBalance_WithValidAmount_ShouldAddBalance()
    {
        // Arrange
        var userId = 1;
        var amountInCents = 5000L;
        var user = CreateUser(userId, balance: 10000);
        _userRepository
            .GetById(userId)
            .Returns(Result.Success<UserEntity?>(user));
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _userService.AddBalance(userId, amountInCents);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Balance.Should().Be(15000);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBalance_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        var amountInCents = 5000L;
        _userRepository
            .GetById(userId)
            .Returns(Result.Success<UserEntity?>(null));

        // Act
        var result = await _userService.AddBalance(userId, amountInCents);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.User.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBalance_WithZeroAmount_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        var amountInCents = 0L;
        var user = CreateUser(userId, balance: 10000);
        _userRepository
            .GetById(userId)
            .Returns(Result.Success<UserEntity?>(user));

        // Act
        var result = await _userService.AddBalance(userId, amountInCents);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.User.InvalidAmount);
    }

    [Fact]
    public async Task AddBalance_WithNegativeAmount_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        var amountInCents = -1000L;
        var user = CreateUser(userId, balance: 10000);
        _userRepository
            .GetById(userId)
            .Returns(Result.Success<UserEntity?>(user));

        // Act
        var result = await _userService.AddBalance(userId, amountInCents);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.User.InvalidAmount);
    }

    [Fact]
    public async Task AddBalance_WhenSaveFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        var amountInCents = 5000L;
        var user = CreateUser(userId, balance: 10000);
        var error = DomainError.DbError.Error(nameof(UserService), "Save failed");
        _userRepository
            .GetById(userId)
            .Returns(Result.Success<UserEntity?>(user));
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure(error));

        // Act
        var result = await _userService.AddBalance(userId, amountInCents);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
    }

    [Fact]
    public async Task GetActiveUsersAsync_WithActiveUsers_ShouldReturnActiveUsernames()
    {
        // Arrange
        var users = new List<UserEntity>
        {
            CreateUser(1, username: "user1"),
            CreateUser(2, username: "user2"),
            CreateUser(3, username: "user3")
        };

        _userRepository
            .GetAllUsersAsync()
            .Returns(Result.Success(users));

        _activityTracker.IsUserActive(1).Returns(true);
        _activityTracker.IsUserActive(2).Returns(true);
        _activityTracker.IsUserActive(3).Returns(false);

        // Act
        var result = await _userService.GetActiveUsersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain("user1");
        result.Value.Should().Contain("user2");
        result.Value.Should().NotContain("user3");
    }

    [Fact]
    public async Task GetActiveUsersAsync_WhenRepositoryFails_ShouldReturnFailure()
    {
        // Arrange
        var error = DomainError.User.NotFound;
        _userRepository
            .GetAllUsersAsync()
            .Returns(Result.Failure<List<UserEntity>>(error));

        // Act
        var result = await _userService.GetActiveUsersAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
    }

    [Fact]
    public async Task GetActiveUsersAsync_WithNoActiveUsers_ShouldReturnEmptyList()
    {
        // Arrange
        var users = new List<UserEntity>
        {
            CreateUser(1, username: "user1"),
            CreateUser(2, username: "user2")
        };

        _userRepository
            .GetAllUsersAsync()
            .Returns(Result.Success(users));

        _activityTracker.IsUserActive(Arg.Any<int>()).Returns(false);

        // Act
        var result = await _userService.GetActiveUsersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private static UserEntity CreateUser(int id, long balance = 0, string username = "testuser")
    {
        return new UserEntity
        {
            Id = id,
            Username = username,
            PasswordHash = "hash",
            Balance = balance,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static List<BetEntity> CreateBets(int userId, int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new BetEntity
            {
                Id = i,
                UserId = userId,
                BetString = $"bet{i}",
                BetAmountInCents = 1000,
                WinningNumber = 10,
                WonAmountInCents = 0,
                SpinId = Guid.NewGuid(),
                IpAddress = "127.0.0.1",
                CreatedAt = DateTime.UtcNow
            })
            .ToList();
    }
}
