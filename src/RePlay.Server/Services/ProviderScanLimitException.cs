namespace RePlay.Server.Services;

/// <summary>Raised when a bounded provider scan cannot return a complete result within its page limit.</summary>
public sealed class ProviderScanLimitException(string provider, string message) : InvalidOperationException(message)
{
    public string Provider { get; } = provider;
}
