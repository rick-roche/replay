# TypeScript Development

- Follow the repository's TypeScript target and Vite configuration rather than generic runtime assumptions.
- Preserve strict typing. Avoid `any`; narrow `unknown` data received from storage or external services.
- Reuse generated API schemas and existing domain types instead of duplicating response shapes.
- Follow established PascalCase component filenames and nearby module naming conventions.
- Use `async`/`await`, surface actionable user-facing errors through established context/API handling, and test error paths.
- Run `npm run validate` and `npm run test` after frontend changes.
