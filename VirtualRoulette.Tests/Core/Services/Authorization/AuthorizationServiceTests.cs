using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Security.Claims;
using VirtualRoulette.Core.Entities;
using VirtualRoulette.Core.Services.ActivityTracker;
using VirtualRoulette.Core.Services.Authorization;
using VirtualRoulette.Core.Services.PasswordHasher;
using VirtualRoulette.Infrastructure.Persistence.Repositories;
using VirtualRoulette.Infrastructure.SignalR;
using VirtualRoulette.Infrastructure.SignalR.Hubs;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Shared.Errors;
using VirtualRoulette.Shared.Result;
using Xunit;
using Entities = VirtualRoulette.Core.Entities;

namespace VirtualRoulette.Tests.Core.Services.Authorization;

public class AuthorizationServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IUserRepository _validatorUserRepository;
    private readonly AuthorizationServiceValidator _validator;
    private readonly IUserActivityTracker _activityTracker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<JackpotHub> _hubContext;
    private readonly IJackpotHubConnectionTracker _connectionTracker;
    private readonly IOptions<SignalRSettings> _signalRSettings;
    private readonly AuthorizationService _authorizationService;
    private readonly HttpContext _httpContext;

    public AuthorizationServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _validatorUserRepository = Substitute.For<IUserRepository>();
        _passwordHasherService = Substitute.For<IPasswordHasherService>();
        _validator = new AuthorizationServiceValidator(_validatorUserRepository);
        _activityTracker = Substitute.For<IUserActivityTracker>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _hubContext = Substitute.For<IHubContext<JackpotHub>>();
        _connectionTracker = Substitute.For<IJackpotHubConnectionTracker>();
        _signalRSettings = Options.Create(new SignalRSettings
        {
            HubPath = "/hub",
            JackpotGroupName = "jackpot",
            JackpotUpdatedMethod = "JackpotUpdated",
            ForceDisconnectMethod = "ForceDisconnect"
        });

        _authorizationService = new AuthorizationService(
            _userRepository,
            _passwordHasherService,
            _validator,
            _activityTracker,
            _unitOfWork,
            _hubContext,
            _connectionTracker,
            _signalRSettings
        );

        // Setup HttpContext with mocked service provider for authentication
        var serviceProvider = Substitute.For<IServiceProvider>();
        var authenticationService = Substitute.For<IAuthenticationService>();
        serviceProvider.GetService(typeof(IAuthenticationService)).Returns(authenticationService);
        _httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
    }

    [Fact]
    public async Task Register_WithValidInput_ShouldCreateUser()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        var passwordHash = "hashed_password";
        SetupSuccessfulRegistration(username, password, passwordHash);

        // Act
        var result = await _authorizationService.Register(username, password);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _userRepository.Received(1).CreateAsync(Arg.Any<Entities.User>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WhenCreateUserFails_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        var passwordHash = "hashed_password";
        var error = DomainError.User.NotFound;
        _validatorUserRepository
            .UsernameExistsAsync(username)
            .Returns(Result.Success(false));
        _passwordHasherService
            .HashPassword(password)
            .Returns(Result.Success(passwordHash));
        _userRepository
            .CreateAsync(Arg.Any<Entities.User>())
            .Returns(Result.Failure<Entities.User>(error));

        // Act
        var result = await _authorizationService.Register(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
    }

    [Fact]
    public async Task Register_WhenValidationFails_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        _validatorUserRepository
            .UsernameExistsAsync(username)
            .Returns(Result.Success(true)); // Username exists, validation should fail

        // Act
        var result = await _authorizationService.Register(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<Entities.User>());
    }

    [Fact]
    public async Task Register_WhenPasswordHashFails_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        var error = DomainError.PasswordHasher.HashError;
        _validatorUserRepository
            .UsernameExistsAsync(username)
            .Returns(Result.Success(false)); // Username doesn't exist, validation passes
        _passwordHasherService
            .HashPassword(password)
            .Returns(Result.Failure<string>(error));

        // Act
        var result = await _authorizationService.Register(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
    }


    [Fact]
    public async Task SignIn_WithValidCredentials_ShouldSignInUser()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        var user = CreateUser(1, username);
        SetupSuccessfulSignIn(username, password, user);

        // Act
        var result = await _authorizationService.SignIn(username, password, _httpContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _activityTracker.Received(1).UpdateActivity(user.Id);
    }

    [Fact]
    public async Task SignIn_WhenValidationFails_ShouldReturnFailure()
    {
        // Arrange
        var username = string.Empty;
        var password = "password123";

        // Act
        var result = await _authorizationService.SignIn(username, password, _httpContext);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SignIn_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        var error = DomainError.User.NotFound;
        _userRepository
            .GetByUsernameAsync(username)
            .Returns(Result.Failure<Entities.User>(error));

        // Act
        var result = await _authorizationService.SignIn(username, password, _httpContext);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
    }

    [Fact]
    public async Task SignIn_WithIncorrectPassword_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = "wrongpassword";
        var user = CreateUser(1, username, "correct_hash");
        _userRepository
            .GetByUsernameAsync(username)
            .Returns(Result.Success(user));
        _passwordHasherService
            .VerifyPassword(password, user.PasswordHash)
            .Returns(Result.Success(false));

        // Act
        var result = await _authorizationService.SignIn(username, password, _httpContext);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(DomainError.User.InvalidUser);
    }

    [Fact]
    public async Task SignOut_WithValidUserId_ShouldSignOutUser()
    {
        // Arrange
        var userId = 1;
        var connectionId = "connection123";
        _connectionTracker
            .GetConnection(userId)
            .Returns(connectionId);

        // Act
        var result = await _authorizationService.SignOut(userId, _httpContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _connectionTracker.Received(1).RemoveConnection(userId);
        _activityTracker.Received(1).RemoveUser(userId);
    }

    [Fact]
    public async Task SignOut_WithoutConnection_ShouldStillSignOut()
    {
        // Arrange
        var userId = 1;
        _connectionTracker
            .GetConnection(userId)
            .Returns((string?)null);

        // Act
        var result = await _authorizationService.SignOut(userId, _httpContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // RemoveConnection is only called if connectionId is not null (see implementation)
        _connectionTracker.DidNotReceive().RemoveConnection(userId);
        _activityTracker.Received(1).RemoveUser(userId);
    }

    private void SetupSuccessfulRegistration(string username, string password, string passwordHash)
    {
        _validatorUserRepository
            .UsernameExistsAsync(username)
            .Returns(Result.Success(false)); // Username doesn't exist
        _passwordHasherService
            .HashPassword(password)
            .Returns(Result.Success(passwordHash));
        _userRepository
            .CreateAsync(Arg.Any<Entities.User>())
            .Returns(Result.Success(new Entities.User
            {
                Username = username,
                PasswordHash = passwordHash,
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            }));
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success());
    }

    private void SetupSuccessfulSignIn(string username, string password, Entities.User user)
    {
        _userRepository
            .GetByUsernameAsync(username)
            .Returns(Result.Success(user));
        _passwordHasherService
            .VerifyPassword(password, user.PasswordHash)
            .Returns(Result.Success(true));
    }

    private static Entities.User CreateUser(int id, string username, string passwordHash = "hash")
    {
        return new Entities.User
        {
            Id = id,
            Username = username,
            PasswordHash = passwordHash,
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };
    }
}
