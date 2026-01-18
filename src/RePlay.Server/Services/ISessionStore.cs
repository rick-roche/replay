using System.Collections.Concurrent;
using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Service for managing authentication sessions in memory.
/// Note: For production, this should be replaced with a distributed cache.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Store a session.
    /// </summary>
    void StoreSession(AuthSession session);

    /// <summary>
    /// Retrieve a session by ID.
    /// </summary>
    AuthSession? GetSession(string sessionId);

    /// <summary>
    /// Remove a session.
    /// </summary>
    void RemoveSession(string sessionId);
}

/// <summary>
/// In-memory implementation of session storage.
/// Sessions are stored in a concurrent dictionary.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, AuthSession> _sessions = new();

    public void StoreSession(AuthSession session)
    {
        _sessions[session.SessionId] = session;
    }

    public AuthSession? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    public void RemoveSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}
