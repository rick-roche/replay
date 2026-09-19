# Accessibility

- Use existing Radix components and native HTML semantics before adding ARIA.
- Every interactive control needs an accessible name, keyboard operation, and visible focus state.
- Associate form labels, help text, and validation messages programmatically. Do not rely on color alone for status.
- Use headings and landmarks appropriately for rendered pages; do not impose page-level heading rules on isolated components.
- Keep layouts usable at 320 CSS pixels without page-level horizontal scrolling.
- Test keyboard behavior and accessible labels for changed interactive UI. Manual browser, screen-reader, contrast, and forced-colors checks remain necessary when relevant.
