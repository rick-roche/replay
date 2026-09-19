# Coolify Deployment

Re:Play is deployed as one Coolify Application. GitHub Actions validates the
repository, publishes an immutable GHCR image, and triggers Coolify to deploy
the validated commit. Coolify does not build from Compose and automatic source
deployments must remain disabled.

## Production application

Configure the Coolify resource with:

| Setting | Value |
| --- | --- |
| Resource | Application |
| Image | `ghcr.io/rick-roche/replay` |
| Image tag | Full Git SHA set by GitHub Actions |
| Build pack | Docker Image |
| Port | `8080` |
| Domain | `https://replay.rickroche.com` |
| Health check | `/health` |
| Replicas | `1` |
| Persistent volume | None |

Grant the Coolify server read access to the GHCR package if the package is
private. Disable automatic source deployments so a push cannot deploy before
CI completes.

The server stores authenticated sessions in memory. A restart or replacement
logs users out, and the application must run with one replica. No application
database volume is currently required.

## Environment variables

Set these in Coolify. Never commit provider credentials:

```text
ASPNETCORE_ENVIRONMENT=Production
Spotify__ClientId=<secret>
Spotify__ClientSecret=<secret>
Spotify__RedirectUri=https://replay.rickroche.com/api/auth/callback
Discogs__ConsumerKey=<secret>
Discogs__ConsumerSecret=<secret>
Lastfm__ApiKey=<optional-secret>
SetlistFm__ApiKey=<optional-secret>
```

Spotify credentials and redirect URI are required. The other provider values
are required when their integrations are used.

## GitHub Actions

The `CI` workflow runs on pull requests and pushes to `main`. A successful
`main` run builds and publishes:

```text
ghcr.io/rick-roche/replay:<full-git-sha>
```

The `Deploy to Coolify` workflow runs only after that CI workflow succeeds. It
updates the Coolify application tag and triggers a deployment through the
Coolify API.

Required GitHub repository secrets:

```text
COOLIFY_API_URL
COOLIFY_APPLICATION_UUID
COOLIFY_TOKEN
```

## Automated releases

Release Please runs after successful `main` CI and maintains a release pull
request from Conventional Commit history. A successful CI run on that release
branch automatically merges it using `RELEASE_PLEASE_TOKEN`. The resulting
`main` commit runs the normal image publication and Coolify deployment, and
Release Please creates the GitHub Release, `vX.Y.Z` tag, and `CHANGELOG.md`.

Configure this additional fine-grained GitHub token secret:

```text
RELEASE_PLEASE_TOKEN
```

Scope it to this repository with Contents, Issues, and Pull requests read/write
permissions. The token is required because GitHub suppresses downstream
workflow events created by the default `GITHUB_TOKEN`.

The first automated release is configured to start at `1.0.0`. Subsequent
versions follow Conventional Commits: `fix:` produces a patch release, `feat:`
produces a minor release, and `!` or `BREAKING CHANGE:` produces a major
release. `docs:`, `chore:`, `refactor:`, and `test:` changes do not release by
themselves.

## DNS and Spotify

Point this DNS record at the Coolify server:

```text
replay.rickroche.com
```

Register this exact Spotify callback URL:

```text
https://replay.rickroche.com/api/auth/callback
```

PR previews are intentionally disabled. Spotify does not support the wildcard
callback setup required for arbitrary preview domains, and production secrets
must not be exposed to pull-request builds.

## Local container check

The root `Dockerfile` is provider-neutral and can be tested locally:

```bash
docker build -t replay:test .
docker run --rm -p 8080:8080 \
  -e Spotify__ClientId=test \
  -e Spotify__ClientSecret=test \
  -e Spotify__RedirectUri=https://replay.rickroche.com/api/auth/callback \
  replay:test
```

Then request `http://127.0.0.1:8080/health`. The health endpoint checks only
process readiness and does not call external providers.

## Migration order

1. Create the Coolify Application and configure its environment and GHCR access.
2. Add the GitHub secrets listed above.
3. Point `replay.rickroche.com` at Coolify.
4. Confirm `/health` and Spotify login on the new deployment.
5. Decommission the old Azure Container App and related resources.
