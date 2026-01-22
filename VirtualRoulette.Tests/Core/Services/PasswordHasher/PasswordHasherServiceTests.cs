using FluentAssertions;
using VirtualRoulette.Core.Services.PasswordHasher;
using VirtualRoulette.Shared.Errors;
using Xunit;

namespace VirtualRoulette.Tests.Core.Services.PasswordHasher;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _passwordHasherService;

    public PasswordHasherServiceTests()
    {
        _passwordHasherService = new PasswordHasherService();
    }

    [Fact]
    public void HashPassword_WithValidPassword_ShouldReturnSuccess()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var result = _passwordHasherService.HashPassword(password);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        var parts = result.Value.Split(':');
        parts.Should().HaveCount(3);
        parts[0].Should().Be("100000");
    }

    [Fact]
    public void HashPassword_WithNullPassword_ShouldReturnFailure()
    {
        // Arrange
        string? password = null;

        // Act
        var result = _passwordHasherService.HashPassword(password!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().Be(DomainError.PasswordHasher.InvalidPassword);
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ShouldReturnFailure()
    {
        // Arrange
        var password = string.Empty;

        // Act
        var result = _passwordHasherService.HashPassword(password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().Be(DomainError.PasswordHasher.InvalidPassword);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashResult = _passwordHasherService.HashPassword(password);
        var hash = hashResult.Value;

        // Act
        var result = _passwordHasherService.VerifyPassword(password, hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashResult = _passwordHasherService.HashPassword(password);
        var hash = hashResult.Value;

        // Act
        var result = _passwordHasherService.VerifyPassword(wrongPassword, hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithNullPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashResult = _passwordHasherService.HashPassword(password);
        var hash = hashResult.Value;

        // Act
        var result = _passwordHasherService.VerifyPassword(null!, hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithNullHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var result = _passwordHasherService.VerifyPassword(password, null!);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithInvalidHashFormat_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "invalid-hash-format";

        // Act
        var result = _passwordHasherService.VerifyPassword(password, invalidHash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ForSamePassword_ShouldProduceDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordHasherService.HashPassword(password);
        var hash2 = _passwordHasherService.HashPassword(password);

        // Assert
        hash1.IsSuccess.Should().BeTrue();
        hash2.IsSuccess.Should().BeTrue();
        hash1.Value.Should().NotBe(hash2.Value);
    }

    [Fact]
    public void VerifyPassword_WithBothHashes_ShouldVerifyCorrectly()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash1 = _passwordHasherService.HashPassword(password);
        var hash2 = _passwordHasherService.HashPassword(password);

        // Act
        var verify1 = _passwordHasherService.VerifyPassword(password, hash1.Value);
        var verify2 = _passwordHasherService.VerifyPassword(password, hash2.Value);

        // Assert
        verify1.IsSuccess.Should().BeTrue();
        verify1.Value.Should().BeTrue();
        verify2.IsSuccess.Should().BeTrue();
        verify2.Value.Should().BeTrue();
    }
}
