# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]

**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Implement a Next.js (App Router) frontend for the existing HelpDisk ASP.NET Core API. The application will use TypeScript with a strict configuration, Tailwind CSS for styling, and organize code by feature slices (`auth`, `tickets`, `agents`, etc.). All API communication will be centralized through a typed `fetch` client to enforce auth token attachment and global error handling.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: TypeScript 5+ (Next.js App Router)

**Primary Dependencies**: Next.js, Tailwind CSS, react-hook-form, zod

**Storage**: HTTP-only Cookies (for JWT auth tokens)

**Testing**: Jest and React Testing Library

**Target Platform**: Web (Modern Browsers, Responsive 375px to 1440px+)

**Project Type**: Next.js Web Application

**Performance Goals**: Fast client-side transitions, < 2s for list filtering/sorting

**Constraints**: Must strictly consume existing ASP.NET Core API without duplicating business logic

**Scale/Scope**: ~10 features (auth, tickets, comments, etc.), ~15 pages

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Backend Is the Source of Truth**: Passes. API clients merely fetch and mutate via standard HTTP methods.
- **II. TypeScript Strictness**: Passes. Types and Zod schemas defined for all models.
- **III. Feature-Based Code Organisation**: Passes. Using `src/features/*`, `src/components/*`, etc.
- **IV. API Communication Layer**: Passes. Centralized fetch client in `src/lib/api/client.ts`.
- **V. Authentication Storage**: Passes. Decided on HTTP-only Cookies via Next.js Server Actions.
- **VI-X. UI & Styling Rules**: Passes. Forms via `react-hook-form` and `zod`, styling via Tailwind CSS (as explicitly requested by user).

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
frontend/helpdesk-web/
├── src/
│   ├── app/                 # Next.js App Router (Pages & Layouts)
│   ├── features/            # Feature-sliced domains
│   │   ├── auth/
│   │   ├── tickets/
│   │   ├── comments/
│   │   ├── attachments/
│   │   ├── agents/
│   │   ├── categories/
│   │   └── reports/
│   ├── components/          # Shared UI primitives (Tailwind CSS)
│   ├── lib/                 # Shared utilities, API client wrapper
│   ├── hooks/               # Shared React hooks
│   └── types/               # Global TypeScript types (API DTOs)
└── tests/
```

**Structure Decision**: Selected the feature-based structure as mandated by the HelpDisk Frontend Constitution (Principle III). The frontend application lives inside `frontend/helpdesk-web/`.
