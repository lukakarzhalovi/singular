using FluentAssertions;
using NSubstitute;
using VirtualRoulette.Core.Services.Authorization;
using VirtualRoulette.Infrastructure.Persistence.Repositories;
using VirtualRoulette.Shared.Result;
using Xunit;

namespace VirtualRoulette.Tests.Core.Services.Authorization;

public class AuthorizationServiceValidatorTests
{
    private readonly IUserRepository _userRepository;
    private readonly AuthorizationServiceValidator _validator;

    public AuthorizationServiceValidatorTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _validator = new AuthorizationServiceValidator(_userRepository);
    }

    [Fact]
    public async Task ValidateRegistrationAsync_WithValidInput_ShouldReturnSuccess()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        _userRepository
            .UsernameExistsAsync(username)
            .Returns(Result.Success(false));

        // Act
        var result = await _validator.ValidateRegistrationAsync(username, password);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _userRepository.Received(1).UsernameExistsAsync(username);
    }

    [Fact]
    public async Task ValidateRegistrationAsync_WithEmptyUsername_ShouldReturnFailure()
    {
        // Arrange
        var username = string.Empty;
        var password = "password123";

        // Act
        var result = await _validator.ValidateRegistrationAsync(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
        await _userRepository.DidNotReceive().UsernameExistsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ValidateRegistrationAsync_WithWhitespaceUsername_ShouldReturnFailure()
    {
        // Arrange
        var username = "   ";
        var password = "password123";

        // Act
        var result = await _validator.ValidateRegistrationAsync(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRegistrationAsync_WithEmptyPassword_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = string.Empty;

        // Act
        var result = await _validator.ValidateRegistrationAsync(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRegistrationAsync_WithExistingUsername_ShouldReturnFailure()
    {
        // Arrange
        var username = "existinguser";
        var password = "password123";
        _userRepository
            .UsernameExistsAsync(username)
            .Returns(Result.Success(true));

        // Act
        var result = await _validator.ValidateRegistrationAsync(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
        await _userRepository.Received(1).UsernameExistsAsync(username);
    }

    [Fact]
    public async Task ValidateRegistrationAsync_WhenRepositoryFails_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";
        var error = VirtualRoulette.Shared.Errors.DomainError.User.NotFound;
        _userRepository
            .UsernameExistsAsync(username)
            .Returns(Result.Failure<bool>(error));

        // Act
        var result = await _validator.ValidateRegistrationAsync(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error);
    }

    [Fact]
    public void ValidateSignIn_WithValidInput_ShouldReturnSuccess()
    {
        // Arrange
        var username = "testuser";
        var password = "password123";

        // Act
        var result = AuthorizationServiceValidator.ValidateSignIn(username, password);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateSignIn_WithEmptyUsername_ShouldReturnFailure()
    {
        // Arrange
        var username = string.Empty;
        var password = "password123";

        // Act
        var result = AuthorizationServiceValidator.ValidateSignIn(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ValidateSignIn_WithEmptyPassword_ShouldReturnFailure()
    {
        // Arrange
        var username = "testuser";
        var password = string.Empty;

        // Act
        var result = AuthorizationServiceValidator.ValidateSignIn(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ValidateSignIn_WithWhitespaceInput_ShouldReturnFailure()
    {
        // Arrange
        var username = "   ";
        var password = "   ";

        // Act
        var result = AuthorizationServiceValidator.ValidateSignIn(username, password);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
