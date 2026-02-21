# Deploy RePlay to Azure Container Apps (using GHCR)

This guide deploys RePlay to Azure Container Apps (ACA) while storing images in GitHub Container Registry (GHCR).

## What this repo now does

- Uses Docker Buildx to push image(s) to GHCR.
- Uses `azd` for Azure environment + infrastructure provisioning, including the Container App resource.
- Avoids Azure Container Registry (ACR) for runtime images.

Implementation references:
- `azure.yaml`
- `infra/main.bicep`
- `.github/workflows/deploy-aca-ghcr.yml`
- `src/RePlay.AppHost/AppHost.cs`
- `Dockerfile`

## 1. Prepare Azure identity for GitHub Actions (OIDC)

Create an Entra app registration/service principal with permissions to your target subscription/resource group.

Add these repository secrets:
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

## 2. Prepare GHCR pull credentials for ACA

ACA needs long-lived credentials to pull from GHCR.

1. Create a GitHub Personal Access Token with at least:
   - `read:packages`
2. Add repository secrets:
   - `GHCR_USERNAME` (GitHub username or org bot user)
   - `GHCR_PAT` (PAT value)

## 3. Configure app secrets

Required secrets:
- `SPOTIFY_CLIENT_ID`
- `SPOTIFY_CLIENT_SECRET`

Required variable:
- `SPOTIFY_REDIRECT_URI`

The deploy workflow validates `SPOTIFY_REDIRECT_URI` and fails early unless it:
- starts with `https://`
- does not contain `localhost`

Optional source provider secrets:
- `LASTFM_API_KEY`
- `DISCOGS_CONSUMER_KEY`
- `DISCOGS_CONSUMER_SECRET`
- `SETLISTFM_API_KEY`

## 4. Configure deploy defaults (optional)

Add repository variables (`Settings -> Secrets and variables -> Actions -> Variables`):
- `AZURE_LOCATION` (default: `uksouth`)
- `AZURE_RESOURCE_GROUP` (default: `replay-rg`)
- `AZD_ENV_NAME` (default: `prod`)
- `ACA_ENV_NAME` (default: `replay-env`)
- `ACA_APP_NAME` (default: `replay`)
- `GHCR_REPOSITORY` (default: `${owner}/${repo}`)
- `ACA_CUSTOM_DOMAIN` (optional, example: `replay.rickroche.com`)

## 5. Run deployment workflow

Workflow: `Deploy ACA from GHCR`

Trigger options:
- Auto on push to `main`.
- Manual from GitHub Actions tab (`workflow_dispatch`).

Manual inputs:
- `run_push` (default `true`): run Docker Buildx push before deploy.
- `container_image` (optional): override image reference if needed.
  - Default used by workflow: `ghcr.io/<GHCR_REPOSITORY>/server:sha-<commit>`

High-level workflow sequence:
1. Build and push image(s) with Docker Buildx to GHCR, tagged as `sha-<commit>`.
2. `azd auth login` + `azd provision` provisions/updates infra from `infra/main.bicep` (ACA environment + app).

## 6. Set Spotify redirect URI

After deploy, workflow output shows the ACA URL.

Set Spotify redirect URI to:
- `https://<your-aca-fqdn>/api/auth/callback`

Ensure `SPOTIFY_REDIRECT_URI` variable matches this exact URI.

If using `ACA_CUSTOM_DOMAIN`, set the redirect URI to:
- `https://<your-custom-domain>/api/auth/callback`

## 7. Optional custom domain (Bicep-managed)

When `ACA_CUSTOM_DOMAIN` is set, Bicep configures ingress custom domain with `bindingType: 'Auto'` and a managed certificate.

Notes:
- DNS ownership/validation records must exist and be propagated before deployment succeeds.
- If deploy fails on custom-domain validation, create/verify DNS records and re-run the workflow.

## Notes

- CI does not call `aspire do push`, which avoids Aspire's ACR provisioning path.
- The API validates Spotify configuration at startup (`ValidateOnStart`), so invalid auth config fails fast during startup/deploy.
- ACA is configured with `min-replicas=0` and `max-replicas=1` for low-cost hobby usage.
- Infrastructure config omits ACA environment log routing to minimize recurring logging cost.
- If you need to deploy a non-default image/tag, pass it in `container_image` when dispatching the workflow.
