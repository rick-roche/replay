# Spotify OAuth Configuration

## Prerequisites

1. Create a Spotify Developer account at https://developer.spotify.com
2. Create a new application in the Spotify Developer Dashboard

## Important: Redirect URI Requirements

⚠️ **Spotify does NOT allow `localhost` as a redirect URI.**

According to [Spotify's documentation](https://developer.spotify.com/documentation/web-api/concepts/redirect_uri):
- Use explicit IPv4 loopback: `http://127.0.0.1:PORT`
- Or explicit IPv6 loopback: `http://[::1]:PORT`
- HTTPS is required for all other addresses

## Configuration Steps

### 1. Register Your Application

1. Go to https://developer.spotify.com/dashboard
2. Click "Create an App"
3. Fill in the details:
   - **App name**: Re:Play (or your preferred name)
   - **App description**: A playlist management application
   - **Redirect URI**: See "Configure Your Redirect URI" section below
4. Accept the terms and create the app

### 2. Get Your Credentials

From your app's dashboard:
1. Note your **Client ID**
2. Click "Show Client Secret" to reveal your **Client Secret**
3. Keep these secure - never commit them to source control

### 3. Configure Your Redirect URI

#### Find Your Local Port

When running with `aspire run`, find the port for the API service:
1. Check the Aspire dashboard (usually http://localhost:17500)
2. Look for the RePlay.Server service
3. Note the HTTPS port (e.g., 7286)

#### Register Redirect URI in Spotify

1. Go to your app settings on Spotify Developer Dashboard
2. Add redirect URI: `http://127.0.0.1:7286/api/auth/callback`
   - Replace `7286` with your actual port
   - Use `127.0.0.1` NOT `localhost`

### 4. Configure Application

Create `src/RePlay.Server/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Spotify": {
    "ClientId": "your-client-id-here",
    "ClientSecret": "your-client-secret-here",
    "RedirectUri": "http://127.0.0.1:7286/api/auth/callback"
  }
}
```

Also create `src/RePlay.Server/appsettings.json` (base settings):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Important**: These files are in `.gitignore` and will not be committed.

### 5. Update for Production

For production deployment:
1. Register HTTPS redirect URI in Spotify: `https://your-domain.com/api/auth/callback`
2. Update application configuration with production URI

## Environment Variables (Alternative)

Instead of configuration files, you can set environment variables:

```bash
export Spotify__ClientId="your_client_id"
export Spotify__ClientSecret="your_client_secret"
export Spotify__RedirectUri="http://127.0.0.1:7286/api/auth/callback"
```

Or in PowerShell:
```powershell
$env:Spotify__ClientId = "your_client_id"
$env:Spotify__ClientSecret = "your_client_secret"
$env:Spotify__RedirectUri = "http://127.0.0.1:7286/api/auth/callback"
```

## Troubleshooting

### "localhost is not allowed" Error

If you see an error mentioning localhost is not allowed, update your Redirect URI to use `127.0.0.1` instead.

### Configuration Not Found

If you see `ArgumentNullException` when accessing `/api/auth/login`:

1. Verify `appsettings.Development.json` exists in `src/RePlay.Server/`
2. Check all three fields are present: `ClientId`, `ClientSecret`, `RedirectUri`
3. Ensure values are not empty
4. Port number matches your local setup
5. Restart the application

### "Code exchange failed"

If Spotify returns a redirect URI mismatch error:

1. Verify the redirect URI in your Spotify app settings matches exactly
2. Use `http://127.0.0.1:PORT` NOT `https://` for local development
3. Port numbers must match between Spotify settings and configuration

## Verification

Run the application:
```bash
aspire run
```

Navigate to `http://127.0.0.1:7286/api/auth/login` - you should be redirected to Spotify's login page.
