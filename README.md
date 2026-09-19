# Re:Play

[![CI](https://github.com/rick-roche/replay/actions/workflows/ci.yml/badge.svg)](https://github.com/rick-roche/replay/actions/workflows/ci.yml) [![Deploy to Coolify](https://github.com/rick-roche/replay/actions/workflows/deploy.yml/badge.svg)](https://github.com/rick-roche/replay/actions/workflows/deploy.yml)

![Re:Play logo](src/frontend/public/replay-logo.svg)

Re:Play is a music-first social app that lets users explore, queue, and share listening sessions using provider authentication (e.g., Spotify). The project contains a .NET backend, an Aspire orchestrator, and a React + TypeScript frontend.

## Technology stack

- Backend: .NET 10.0, C# 14
- Orchestration: Aspire
- Frontend: React, TypeScript, Vite
- Node: v24
- Testing: xUnit (backend), vitest (frontend)
- CI: GitHub Actions
- Deployment: Coolify Application with immutable GHCR images

## Quickstart (development)

Prerequisites:
- .NET 10 SDK
- Node.js 24+
- npm
- Git

Clone the repo:

```sh
    git clone https://github.com/rick-roche/replay.git
    cd replay
```

Run the full application (recommended):

```sh
    aspire run
```

Backend build & tests:

```sh
    dotnet build RePlay.sln -c Release
    dotnet test
```

Frontend (from repo root):

```sh
    cd src/frontend
    npm install
    npm run build
    npm run validate   # lint & build checks
    npm run test
```

## CI and coverage

CI is defined in [.github/workflows/ci.yml](./.github/workflows/ci.yml) and runs backend and frontend builds, tests, and uploads coverage artifacts for inspection.

## Deployment

Deploy to Coolify using immutable images stored in GitHub Container Registry:

- Guide: [docs/deployment.md](./docs/deployment.md)
- Workflow: [.github/workflows/deploy.yml](./.github/workflows/deploy.yml)

## Contributing

Contributions welcome — please open issues or pull requests with a clear description of changes and ensure tests pass.

## License

See the [LICENSE](./LICENSE) file in the repository root.
