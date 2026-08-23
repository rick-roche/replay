# React Development

- Use TypeScript function components, existing contexts, Radix Themes, and the repository's Tailwind/CSS patterns.
- Preserve the dark, web-first responsive interface. Verify layouts at narrow viewport widths when changing UI.
- Prefer derived state. Do not set state during render or conditionally in effect bodies.
- Use effects for synchronization with external systems and clean them up where needed. Protect asynchronous UI requests from stale results.
- Use the generated OpenAPI client and existing API wrappers rather than duplicating contracts or adding data-fetching dependencies.
- Write behavior-focused tests with Vitest and Testing Library. Follow nearby test patterns.
