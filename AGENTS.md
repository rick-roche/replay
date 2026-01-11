# Agent Instructions

Instructions for agents on how they must work with this repository.

When acting on this repository:

- Follow this file strictly.
- Ask before making architectural changes.
- Keep PRs and changes small and focused.
- Explain non-obvious decisions in comments.
- Default to the simplest working solution.

## Core Principles

- Prefer boring, explicit code over clever abstractions.
- Minimize dependencies and framework magic.
- Small, typed, composable functions over large classes.
- Data correctness beats UI polish.

## Repository Overview

### Technology Stack

- [Aspire](https://aspire.dev/).
- Backend: .NET 10.0 + C# 14 with xUnit SDK v3 with Microsoft.Testing.Platform for testing.
- Frontend: React + Typescript + Vite with [vitest](https://vitest.dev/) for testing.
- Storage: Browser storage only.
- Infra: Azure Container Apps with GitHub Container Registry.
- Auth: Use the selected music provider for authentication (e.g. Spotify).

### Directory Structure

- `src/`: All application source code.
    - `frontend/`: All the frontend code.
    - `RePlay.AppHost`: Aspire orchestrator.
    - `RePlay.Api`: The exposed Re:Play API
    - `RePlay.Application`: The core application of Re:Play
- `docs/`: All docs other than the README.md.
- `infra/`: All infrastructure needed to deploy the application.

### Setup commands

- `aspire run`: Runs the application (including installing all dependencies).
- `dotnet test`: Runs the tests.

---

## General Rules

- Make only high confidence suggestions when reviewing code changes.
- Always use the latest version C#, currently C# 14 features.
- Never change global.json unless explicitly asked to.

## Frontend Rules

- Dark mode by default.
- Web-first layout, responsive for mobile.

## TypeScript Rules

- Strict typing enabled.
- Avoid `any`.
- Generate frontend types from the API. See https://johnnyreilly.com/dotnet-openapi-and-openapi-ts.

## API Rules

- Use Microsoft.AspNetCore.OpenApi to generate OpenAPI spec and [Scalar](https://github.com/scalar/scalar) to view it.
- All API responses are JSON with explicit HTTP status codes.
- Error responses must include a machine-readable error code.
- Validate inputs defensively.
- Never trust client-provided data blindly.

## Testing Rules

- Prefer non-integrated tests always.
- All backend functionality should have unit tests.
- All frontend components performing logic should have component unit tests
- All API's should have non-integrated contract tests.
