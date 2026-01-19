# Frontend Setup

## Technology Stack

- **Build Tool**: Vite
- **Framework**: React 19 with TypeScript
- **UI Library**: Radix Themes (complete design system)
- **Styling**: Tailwind CSS v4 (utility classes only)
- **Icons**: Lucide React

## Project Structure

```
src/
├── App.tsx           # Main application component
├── main.tsx          # Application entry point
└── index.css         # Global styles and imports
```

## Key Features

- **Dark Mode by Default**: Following AGENTS.md requirements
- **Type-Safe Components**: Full TypeScript support with strict mode
- **Accessible**: Built with Radix Themes components
- **Modern Styling**: Radix Themes with green accent color
- **No Custom Components**: Using Radix Themes directly, not reinventing the wheel

## Development

```bash
# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

## Radix Themes

All UI components come from Radix Themes, configured with:

- **Appearance**: Dark mode
- **Accent Color**: Green (Spotify-aligned)
- **Radius**: Medium

### Available Components

Use any component from [Radix Themes](https://www.radix-ui.com/themes/docs/overview/getting-started):

```tsx
import { Button, Card, Text, Flex, Container, etc } from '@radix-ui/themes'
```

Common components include:
- Layout: Box, Flex, Grid, Container, Section
- Typography: Heading, Text
- Interactive: Button, Link, IconButton
- Display: Card, Badge, Avatar
- Forms: TextField, TextArea, Select, Checkbox, etc.
- Overlay: Dialog, DropdownMenu, Popover, etc.

## Styling

Radix Themes provides the base styling. Tailwind is used only for utility classes where needed (spacing, custom layouts, etc.).
