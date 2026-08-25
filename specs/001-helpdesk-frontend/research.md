# Research & Technical Decisions: HelpDisk Frontend

## Decision 1: Authentication Storage
**Decision**: Use HTTP-only cookies (via Next.js Server Actions or Middleware) for JWT token storage, or `localStorage` if strictly client-side. Given Next.js App Router is used, storing the token in a cookie allows server components and middleware to read the token and protect routes before rendering.
**Rationale**: The constitution states auth storage must be documented. Next.js App Router works best when middleware can intercept unauthorized requests (FR-004, FR-005). Cookies make this possible.
**Alternatives considered**: `localStorage` (only accessible client-side, making server-side rendering and middleware route protection difficult).

## Decision 2: Styling Framework
**Decision**: Use Tailwind CSS.
**Rationale**: The user explicitly requested Tailwind CSS in the command (`Use the following frontend technologies: Next.js, TypeScript, App Router, Tailwind CSS`). This overrides the initial constitution default of Vanilla CSS.
**Alternatives considered**: Vanilla CSS / CSS Modules (original constitution default, but rejected by explicit user prompt).

## Decision 3: Component Architecture & Feature Slicing
**Decision**: Feature-sliced design (`src/features/*`).
**Rationale**: Follows Principle III of the constitution. `auth`, `tickets`, `agents`, `categories`, `reports` will be the top-level features. Shared UI components go to `src/components`.
**Alternatives considered**: Organizing by technical layer (`components/`, `pages/`, `services/`) was rejected as it violates the constitution.

## Decision 4: API Client
**Decision**: Create a centralized `fetch` wrapper in `src/lib/api/client.ts`.
**Rationale**: Follows Principle IV of the constitution. No random fetch requests in UI components. The client will handle attaching the Bearer token (from cookies) and parsing ProblemDetails errors.
**Alternatives considered**: Using `axios` (adds unnecessary bundle size when `fetch` is native and sufficient) or ad-hoc fetches (violates constitution).

## Decision 5: Form Validation
**Decision**: Use `react-hook-form` with `zod`.
**Rationale**: Mandated by the constitution (Principle VIII) and is the industry standard for Next.js applications.
**Alternatives considered**: Formik, custom validation logic.
