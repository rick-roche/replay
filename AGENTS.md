# Agent Instructions

This file defines repository-wide instructions for AI coding agents working in Re:Play.

Re:Play is a music-focused app with an Aspire app host, a .NET 10/C# 14 backend API (`src/RePlay.Server`), and a React + TypeScript + Vite frontend (`src/frontend`).

## Priorities

- Follow this file strictly.
- Keep changes small, focused, and easy to review.
- Default to the simplest working solution.
- Prefer explicit, typed, composable code over abstractions.
- Data correctness beats UI polish.

## Always

- Ask before making architectural changes.
- Keep suggestions high confidence and grounded in existing code patterns.
- Explain non-obvious decisions with concise comments in code.
- Use C# 14 features where appropriate.
- Preserve strict TypeScript typing and avoid `any`.
- Keep frontend dark mode by default and responsive (web-first, mobile-friendly).
- Validate inputs defensively and never trust client-provided data.
- Return JSON API responses with explicit HTTP status codes.
- Include machine-readable error codes for API error responses.
- Regenerate frontend API client when API contract changes:
  - `cd src/frontend && npm run generate-client`
  - Updates `src/frontend/src/api/generated-client.ts`

## Ask First

- Any architecture or major pattern change.
- Adding new dependencies when existing libraries can solve the problem.
- Changing CI, deployment, or coverage policy.
- Changing public API shapes in ways that may break consumers.

## Never

- Never change `global.json` unless explicitly asked.
- Never reduce thresholds in `src/frontend/vitest.config.ts`.
- Never merge code that leaves failing tests, lint, or build checks.

## Repository Map

- `src/RePlay.AppHost`: Aspire orchestrator.
- `src/RePlay.Server`: Backend API.
- `src/RePlay.Server.Tests`: Backend tests.
- `src/frontend`: Frontend app (React/TypeScript/Vite + Vitest).
- `docs/`: Supporting documentation.
- `infra/`: Deployment infrastructure.

## Standard Commands

- Run full app: `aspire run`
- Build backend: `dotnet build RePlay.sln -c Release`
- Test backend: `dotnet test`
- Validate frontend: `cd src/frontend && npm run validate`
- Test frontend: `cd src/frontend && npm run test`

## Testing Expectations

- Prefer non-integrated tests.
- Add or update backend unit tests for backend logic changes.
- Add or update frontend component tests for UI logic changes.
- Add or update API contract-style tests for API behavior changes.
- Keep backend coverage gates passing (`scripts/validate-coverage.sh` in CI).

Before completing work, run and pass:

- `dotnet test`
- `cd src/frontend && npm run validate`
- `cd src/frontend && npm run test`

## Progressive Disclosure

Keep this file lean. Put domain-specific detail in focused docs, then reference them:

- `README.md` for repo setup and workflows.
- `docs/configuration.md` for runtime configuration.
- `docs/deploy-aca-ghcr.md` for deployment flow.
