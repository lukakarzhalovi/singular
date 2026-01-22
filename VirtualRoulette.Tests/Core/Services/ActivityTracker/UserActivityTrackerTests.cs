using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VirtualRoulette.Core.Services.ActivityTracker;
using VirtualRoulette.Presentation.Configuration.Settings;
using Xunit;

namespace VirtualRoulette.Tests.Core.Services.ActivityTracker;

public class UserActivityTrackerTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<ActivityTrackingSettings> _settings;
    private readonly UserActivityTracker _activityTracker;

    public UserActivityTrackerTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _settings = Options.Create(new ActivityTrackingSettings
        {
            InactivityTimeoutMinutes = 5,
            CacheKeyPrefix = "activity_"
        });
        _activityTracker = new UserActivityTracker(_memoryCache, _settings);
    }

    [Fact]
    public void UpdateActivity_WithValidUserId_ShouldStoreActivity()
    {
        // Arrange
        var userId = 1;

        // Act
        _activityTracker.UpdateActivity(userId);

        // Assert
        _activityTracker.IsUserActive(userId).Should().BeTrue();
    }

    [Fact]
    public void IsUserActive_WithNoActivity_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = _activityTracker.IsUserActive(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsUserActive_AfterUpdateActivity_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1;
        _activityTracker.UpdateActivity(userId);

        // Act
        var result = _activityTracker.IsUserActive(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RemoveUser_ShouldRemoveUserFromTracking()
    {
        // Arrange
        var userId = 1;
        _activityTracker.UpdateActivity(userId);
        _activityTracker.IsUserActive(userId).Should().BeTrue();

        // Act
        _activityTracker.RemoveUser(userId);

        // Assert
        _activityTracker.IsUserActive(userId).Should().BeFalse();
    }

    [Fact]
    public void UpdateActivity_WithMultipleUsers_ShouldTrackSeparately()
    {
        // Arrange
        var userId1 = 1;
        var userId2 = 2;

        // Act
        _activityTracker.UpdateActivity(userId1);
        _activityTracker.RemoveUser(userId2);

        // Assert
        _activityTracker.IsUserActive(userId1).Should().BeTrue();
        _activityTracker.IsUserActive(userId2).Should().BeFalse();
    }

    [Fact]
    public void UpdateActivity_ShouldUpdateExistingActivity()
    {
        // Arrange
        var userId = 1;
        _activityTracker.UpdateActivity(userId);

        // Act
        _activityTracker.UpdateActivity(userId);

        // Assert
        _activityTracker.IsUserActive(userId).Should().BeTrue();
    }
}
