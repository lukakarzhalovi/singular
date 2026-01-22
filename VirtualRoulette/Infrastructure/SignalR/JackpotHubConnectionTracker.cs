using System.Collections.Concurrent;

namespace VirtualRoulette.Infrastructure.SignalR;

public interface IJackpotHubConnectionTracker
{
    string? GetConnection(int userId);
    string? SetConnection(int userId, string connectionId);
    string? RemoveConnection(int userId);
}

public class JackpotHubConnectionTracker : IJackpotHubConnectionTracker
{
    private readonly ConcurrentDictionary<int, string> _userToConnection = new();

    public string? GetConnection(int userId)
    {
        return _userToConnection.GetValueOrDefault(userId);
    }

    public string? SetConnection(int userId, string connectionId)
    {
        _userToConnection.TryRemove(userId, out var oldConnectionId);
        _userToConnection[userId] = connectionId;
        return oldConnectionId;
    }

    public string? RemoveConnection(int userId)
    {
        return _userToConnection.TryRemove(userId, out var connectionId) ? connectionId : null;
    }
}