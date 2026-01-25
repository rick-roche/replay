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

    /// <summary>
    /// Store source configuration for a session.
    /// </summary>
    void StoreSourceConfig(string sessionId, ExternalSourceConfig config);

    /// <summary>
    /// Retrieve source configuration for a session.
    /// </summary>
    ExternalSourceConfig? GetSourceConfig(string sessionId, string source);
}

/// <summary>
/// In-memory implementation of session storage.
/// Sessions are stored in concurrent dictionaries.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, AuthSession> _sessions = new();
    private readonly ConcurrentDictionary<(string SessionId, string Source), ExternalSourceConfig> _sourceConfigs = new();

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

        // Remove associated source configs
        var keysToRemove = _sourceConfigs.Keys.Where(k => k.SessionId == sessionId).ToList();
        foreach (var key in keysToRemove)
        {
            _sourceConfigs.TryRemove(key, out _);
        }
    }

    public void StoreSourceConfig(string sessionId, ExternalSourceConfig config)
    {
        _sourceConfigs[(sessionId, config.Source)] = config;
    }

    public ExternalSourceConfig? GetSourceConfig(string sessionId, string source)
    {
        return _sourceConfigs.TryGetValue((sessionId, source), out var config) ? config : null;
    }
}
