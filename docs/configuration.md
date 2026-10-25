# Application Configuration

This document explains how to configure RePlay for local development and production, including Spotify OAuth setup used by the backend.

NOTE: The server enforces that the Spotify Redirect URI must not contain "localhost" (see Program.cs validation) — use `127.0.0.1` or an explicit IPv6 loopback for local development.

## Spotify OAuth (development)

1. Create a Spotify Developer account and register an app at https://developer.spotify.com/dashboard.
2. In your app settings, add a Redirect URI that matches the server's OAuth callback, for example:
   - `http://127.0.0.1:7286/api/auth/callback` (replace port with the one Aspire publishes for the RePlay server)

Important: Spotify accepts explicit loopback addresses for local dev (127.0.0.1 or [::1]). Do not use `localhost` in the registered Redirect URI. For non-local deployments use an HTTPS redirect URI.

The callback always uses `Spotify:RedirectUri`; request and forwarded host headers cannot override it. When the Vite frontend and API have different origins, register and configure the **frontend's public origin** with `/api/auth/callback` so Vite proxies the callback and the session cookie belongs to the browser-facing origin. The post-login return value is a local path, not an external URL.

## Find the local server port

When running with Aspire (aspire run) the AppHost publishes the RePlay server and the Aspire dashboard shows published ports (usually the Aspire dashboard is available at http://localhost:17500). Use the HTTPS port Aspire assigns to the RePlay server and include it in the Redirect URI.

## Configuration files

Local development settings are stored in appsettings.*.json files under src/RePlay.Server/. These files are ignored by git and meant for local secrets and overrides.

Create or update src/RePlay.Server/appsettings.Development.json with Spotify settings:

```json
{
  "Spotify": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RedirectUri": "http://127.0.0.1:7286/api/auth/callback"
  }
}
```

Ensure the RedirectUri value matches exactly the URI registered in the Spotify dashboard and does not contain "localhost".

You can also add other local overrides (Logging, AllowedHosts) as needed; the minimal required fields for OAuth are shown above.

## Environment variables (optional)

You may provide configuration via environment variables instead of appsettings files. Use the ASP.NET configuration key syntax with double-underscore separators:

Bash:

```bash
export Spotify__ClientId="your_client_id"
export Spotify__ClientSecret="your_client_secret"
export Spotify__RedirectUri="http://127.0.0.1:7286/api/auth/callback"
```

PowerShell:

```powershell
$env:Spotify__ClientId = "your_client_id"
$env:Spotify__ClientSecret = "your_client_secret"
$env:Spotify__RedirectUri = "http://127.0.0.1:7286/api/auth/callback"
```

## Production

For the Coolify production deployment register this HTTPS redirect URI in the Spotify dashboard:

- `https://replay.rickroche.com/api/auth/callback`

Provide the production Spotify credentials and RedirectUri through Coolify environment variables or another secure secret store. The application validates that RedirectUri is set and does not contain "localhost".

### Reverse proxy trust

Only loopback proxies are trusted by default. Configure the exact Coolify proxy IP or its dedicated network before deploying:

```text
ReverseProxy__KnownProxies__0=<proxy-IP>
# Or, for a dedicated proxy network:
ReverseProxy__KnownNetworks__0=<proxy-network-CIDR>
AllowedHosts=replay.rickroche.com
```

Do not use `0.0.0.0/0` or a shared untrusted network. Only forwarded client IP and protocol are accepted; forwarded host is ignored. In production, absent an explicit host allowlist, the configured Spotify callback host is used. Health probes must send that host. Trusted forwarding is also necessary for client-IP rate limiting to distinguish anonymous users.

### Session and provider limits

- Application sessions last at most 30 days and are revoked at logout. Access-token refresh preserves the same session and source configuration, including after the access token expires. Restarts still invalidate all sessions; retain the single-replica deployment.
- Source profiles must be configured for the current session; cached browser preferences are not proof of server configuration.
- API calls are limited to 60 requests/minute and four concurrent requests per authenticated Spotify user, or per client IP when anonymous. Rejections return HTTP 429 with `RATE_LIMITED` and `Retry-After`.
- Source and match requests are bounded to 500 items; Discogs and Setlist.fm scans stop at 100 pages rather than silently returning incomplete scans. Use narrower filters when a provider scan limit is reached.
- Spotify search requests use the Development Mode maximum of 10 results. Playlist creation uses `/me/playlists` and `/playlists/{id}/items`. Artist top tracks are unavailable in Development Mode; unsupported responses explicitly ask users to choose tracks or albums instead. Extended Quota Mode can still use artist top tracks.
- Playlist writes are never automatically retried. If creation succeeds but a track batch fails, the API returns HTTP 502 with `isComplete: false`, `PLAYLIST_PARTIALLY_CREATED`, the existing playlist URL, and the number of confirmed track additions. Open that playlist before starting another creation: a timed-out batch may already have been applied.
- If the creation response itself is lost or invalid, HTTP 502 with `PLAYLIST_CREATION_UNCONFIRMED` means Spotify may have created the playlist but its ID is unknown. Check your Spotify library before deliberately starting another playlist; the UI blocks blind resubmission.

## Programmatic validation

Program.cs configures SpotifyOptions with DataAnnotation validation and an extra check that rejects any RedirectUri containing the substring "localhost". If configuration validation fails, the host will not start and errors are logged.

## Troubleshooting

- "Redirect URI mismatch" or "Code exchange failed": ensure the redirect URI registered in Spotify exactly matches the RedirectUri configured in the application (including scheme, host, port, and path).
- "Configuration is invalid" during startup: ensure Spotify ClientId, ClientSecret and RedirectUri are set (either via appsettings or environment variables) and RedirectUri does not include "localhost".
- If the login endpoint fails with ArgumentNullException: verify the development appsettings file exists at src/RePlay.Server/appsettings.Development.json and contains the required Spotify keys.

## Verify locally

Run the whole application:

```bash
aspire run
```

Open the server login endpoint in a browser (replace port as appropriate):

http://127.0.0.1:7286/api/auth/login

You should be redirected to the Spotify authorization page. If not, re-check RedirectUri and credentials.
