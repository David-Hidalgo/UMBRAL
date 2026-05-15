# umbral

This project was created with [Better-T-Stack](https://github.com/AmanVarshney01/create-better-t-stack), a modern TypeScript stack that combines Next.js, Self, and more.

## Features

- **TypeScript** - For type safety and improved developer experience
- **Next.js** - Full-stack React framework
- **ASP.Net** -  Framework used in backend microservices
- **React Native** - Build mobile apps using React
- **Expo** - Tools for React Native development
- **TailwindCSS** - Utility-first CSS for rapid UI development
- **Shared UI package** - shadcn/ui primitives live in `packages/ui`
- **Authentication** - KeyCloack
- **Biome** - Linting and formatting
- **Turborepo** - Optimized monorepo build system

## Getting Started

First, install the dependencies:

```bash
bun install
```

## Database Setup

This project uses SQLite with Drizzle ORM.

1. Start the local SQLite database (optional):

```bash
bun run db:local
```

2. Update your `.env` file in the `apps/web` directory with the appropriate connection details if needed.

3. Apply the schema to your database:

```bash
bun run db:push
```

Then, run the development server:

```bash
bun run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser to see the fullstack application.
Use the Expo Go app to run the mobile application.

## UI Customization

React web apps in this stack share shadcn/ui primitives through `packages/ui`.

- Change design tokens and global styles in `packages/ui/src/styles/globals.css`
- Update shared primitives in `packages/ui/src/components/*`
- Adjust shadcn aliases or style config in `packages/ui/components.json` and `apps/web/components.json`

### Add more shared components

Run this from the project root to add more primitives to the shared UI package:

```bash
npx shadcn@latest add accordion dialog popover sheet table -c packages/ui
```

Import shared components like this:

```tsx
import { Button } from "@umbral/ui/components/button";
```

### Add app-specific blocks

If you want to add app-specific blocks instead of shared primitives, run the shadcn CLI from `apps/web`.

## Git Hooks and Formatting

- Format and lint fix: `bun run check`

## Project Structure

```
umbral/
├── apps/
│   ├── native/      # Mobile application (React Native, Expo)
│   ├── server/      # server API (ASP.NET)
│   └── web/         # Fullstack application (Next.js)
├── packages/
│   ├── ui/          # Shared shadcn/ui components and styles
```

## Available Scripts

- `bun run dev`: Start all applications in development mode
- `bun run build`: Build all applications
- `bun run dev:web`: Start only the web application
- `bun run check-types`: Check TypeScript types across all apps
- `bun run dev:native`: Start the React Native/Expo development server
- `bun run check`: Run Biome formatting and linting

## Start

 ╭───────────────────────────────────────────────────────────────────────────╮
 │                                                                           │
 │  Next steps                                                               │
 │  1. cd umbral                                                             │
 │  2. bun install                                                           │
 │  3. bun run dev                                                           │
 │  Your project will be available at:                                       │
 │  • Frontend: http://localhost:3000                                        │
 │  • Backtend: http://localhost:5018                                        │
 │                                                                           │
 │  NOTE: For Expo connectivity issues, update                               │
 │     apps/native/.env with your local IP address:                          │
 │     EXPO_PUBLIC_SERVER_URL=http://<YOUR_LOCAL_IP>:5018                    │
 │                                                                           │
 │  Linting and formatting:                                                  │
 │  • Format and lint fix: bun run check                                     │
 │                                                                           │
 │  WARNING: 'bun' might cause issues with web + native apps in a monorepo.  │
 │     Use 'pnpm' if problems arise.                                         │
 │                                                                           │
 │  Special sponsors                                                         │
 │  • neondatabase   • Guillermo Rauch   • Clerk   • Novu   • Convex         │
 │                                                                           │
 │  Like Better-T-Stack? Please consider giving us a star                    │
 │     on GitHub:                                                            │
 │  https://github.com/AmanVarshney01/create-better-t-stack                  │
 │                                                                           │
 ╰───────────────────────────────────────────────────────────────────────────╯