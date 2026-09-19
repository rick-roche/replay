# C# Development

- Target .NET 10 and use C# 14 when it improves clarity.
- Follow `.editorconfig`, existing Minimal API patterns, and file-scoped namespaces.
- Validate external input at endpoint boundaries and return explicit JSON API errors with machine-readable codes.
- Keep nullability annotations accurate; use `nameof` for argument names.
- Add XML documentation to public API types and endpoints when it clarifies the contract.
- Test changed service and endpoint behavior using the existing xUnit and FluentAssertions style. Do not add Arrange/Act/Assert comments.
- Add comments only for non-obvious decisions, not routine code.
