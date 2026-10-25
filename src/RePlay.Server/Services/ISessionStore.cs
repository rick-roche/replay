using RePlay.Server.Models;

namespace RePlay.Server.Services;

/// <summary>
/// Service for managing authentication sessions in memory.
/// Sessions are process-local: deployments use one replica and restarts sign users out.
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

    /// <summary>Serialize token rotation and update only a session that is still active.</summary>
    Task<AuthSession?> RefreshSessionAsync(string sessionId,
        Func<string, CancellationToken, Task<AuthSession>> refresh, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of session storage.
/// Each session owns its configuration and refresh lock.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore, IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<string, SessionEntry> _sessions = new();
    private readonly Timer _cleanupTimer;

    public InMemorySessionStore()
    {
        _cleanupTimer = new Timer(_ => CleanupExpiredSessions(), null,
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private sealed class SessionEntry(AuthSession session)
    {
        public AuthSession Session { get; set; } = session;
        public SemaphoreSlim RefreshLock { get; } = new(1, 1);
        public Dictionary<string, ExternalSourceConfig> Configs { get; } = new();
    }

    public void StoreSession(AuthSession session)
    {
        lock (_sync)
        {
            if (_sessions.TryGetValue(session.SessionId, out var entry))
                entry.Session = session;
            else
                _sessions[session.SessionId] = new SessionEntry(session);
        }
    }

    public AuthSession? GetSession(string sessionId)
    {
        lock (_sync)
        {
            return GetActiveEntry(sessionId)?.Session;
        }
    }

    public void RemoveSession(string sessionId)
    {
        lock (_sync)
        {
            _sessions.Remove(sessionId);
        }
    }

    public void StoreSourceConfig(string sessionId, ExternalSourceConfig config)
    {
        lock (_sync)
        {
            if (GetActiveEntry(sessionId) is { } entry)
                entry.Configs[config.Source] = config;
        }
    }

    public ExternalSourceConfig? GetSourceConfig(string sessionId, string source)
    {
        lock (_sync)
        {
            return GetActiveEntry(sessionId) is { } entry && entry.Configs.TryGetValue(source, out var config)
                ? config : null;
        }
    }

    public async Task<AuthSession?> RefreshSessionAsync(string sessionId,
        Func<string, CancellationToken, Task<AuthSession>> refresh, CancellationToken cancellationToken = default)
    {
        SessionEntry entry;
        lock (_sync)
        {
            var active = GetActiveEntry(sessionId);
            if (active == null)
                return null;
            entry = active;
        }

        await entry.RefreshLock.WaitAsync(cancellationToken);
        try
        {
            AuthSession current;
            lock (_sync)
            {
                if (GetActiveEntry(sessionId) != entry)
                    return null;
                current = entry.Session;
            }

            var refreshed = await refresh(current.RefreshToken, cancellationToken);
            lock (_sync)
            {
                // Logout removes the entry immediately, even while Spotify is responding.
                if (GetActiveEntry(sessionId) != entry)
                    return null;
                entry.Session = refreshed with
                {
                    SessionId = current.SessionId,
                    CreatedAt = current.CreatedAt,
                    RefreshToken = string.IsNullOrWhiteSpace(refreshed.RefreshToken) ? current.RefreshToken : refreshed.RefreshToken
                };
                return entry.Session;
            }
        }
        catch (SpotifyRefreshTokenRejectedException)
        {
            lock (_sync)
            {
                // Revoke before releasing the refresh lock so queued requests cannot retry a rejected grant.
                if (_sessions.TryGetValue(sessionId, out var active) && active == entry)
                    _sessions.Remove(sessionId);
            }
            throw;
        }
        finally
        {
            entry.RefreshLock.Release();
        }
    }

    // Called only while holding _sync; removing an entry also releases all retained source configuration.
    private SessionEntry? GetActiveEntry(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
            return null;
        if (!entry.Session.IsSessionExpired())
            return entry;
        _sessions.Remove(sessionId);
        return null;
    }

    private void CleanupExpiredSessions()
    {
        lock (_sync)
        {
            foreach (var sessionId in _sessions.Where(pair => pair.Value.Session.IsSessionExpired()).Select(pair => pair.Key).ToArray())
                _sessions.Remove(sessionId);
        }
    }

    public void Dispose() => _cleanupTimer.Dispose();
}
