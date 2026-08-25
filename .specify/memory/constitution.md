<!--
SYNC IMPACT REPORT
==================
Version change : (blank scaffold) → 1.0.0
Added sections :
  - Core Principles (I–XI, all new)
  - Technology Stack & Constraints (new)
  - Development Workflow & Quality Gates (new)
  - Governance (new)
Modified       : n/a — initial constitution population
Removed        : example/placeholder comments replaced with project-specific content
Deferred TODOs : none — all placeholders resolved
-->

# HelpDisk Frontend Constitution

## Core Principles

### I. Backend Is the Source of Truth (NON-NEGOTIABLE)

The HelpDisk ASP.NET Core API is the sole authority for business rules, data validation,
authorization decisions, and state transitions. The frontend MUST NOT re-implement or
duplicate backend business logic (e.g. ticket status rules, SLA computation, role enforcement).

- All data mutations MUST go through the API; the frontend never mutates local state as a
  substitute for a server round-trip.
- The frontend MUST NOT enforce business invariants (e.g. "cannot assign a closed ticket")
  with client-side guards — it should gracefully handle the `409 Conflict` the server
  returns and surface the `detail` message to the user.
- When the API changes its contract, the frontend MUST adapt; the reverse does not apply.

### II. TypeScript Strictness (NON-NEGOTIABLE)

All source files MUST be `.ts` or `.tsx`. The TypeScript compiler MUST run with
`strict: true`. No `any` type is permitted without an explicit `// eslint-disable` comment
that names the specific exemption and its justification.

- All API response shapes MUST be expressed as TypeScript interfaces or `zod` schemas
  derived from `FRONTEND_API_REFERENCE.md`; untyped JSON objects are forbidden.
- Enums received from the API (`"New"`, `"High"`, `"Pending"`, etc.) MUST be mapped to
  TypeScript string union types or const enums and validated at the API boundary.
- `unknown` is preferred over `any` when the type is genuinely unknown; narrow with
  type guards before use.

### III. Feature-Based Code Organisation

Code is organised by **feature**, not by technical layer.

```
src/
  features/
    auth/          # login, register, token storage
    tickets/       # list, detail, create, edit, state transitions
    comments/      # add & view comments
    attachments/   # upload, download, delete
    categories/    # list (all roles), CRUD (Admin)
    agents/        # CRUD + activate/deactivate (Admin)
    reports/       # admin dashboard reports
  components/      # truly shared, role-agnostic UI primitives
  lib/             # API client, auth helpers, utility functions
  hooks/           # shared React hooks (e.g. useCurrentUser, usePagination)
  types/           # global TypeScript types / API DTOs
```

- A feature folder owns its own components, hooks, types, and API calls.
- Shared UI primitives (Button, Input, Badge, Modal) live in `components/`; they MUST NOT
  import from any feature folder.
- Cross-feature navigation (e.g. a link from a report to a ticket) MUST go through the
  router, not through direct component imports.

### IV. API Communication Layer

All HTTP calls MUST be made through a single typed API client module (`src/lib/api/`).
Direct `fetch`/`axios` calls in components or pages are forbidden.

- The client MUST attach the `Authorization: Bearer <token>` header automatically.
- The client MUST handle `401` globally by clearing stored credentials and redirecting to
  `/login`.
- Error responses MUST be parsed as ProblemDetails (`{ title, detail, status }`); the
  `title` field (the error code) MUST be used for programmatic branching; `detail`
  is for display only.
- Each feature's API calls MUST be isolated in a `<feature>/api.ts` file that calls through
  the shared client — this is the only file allowed to know the endpoint URL.

### V. Authentication & Secure Token Handling

JWT tokens MUST be stored in `localStorage` or an HTTP-only cookie; the chosen strategy
MUST be documented and consistent across the codebase.

- On login, the `role` field from `TokenResponse` MUST be persisted alongside the token;
  it is the authoritative role for UI decisions in the current session.
- Token expiry (`expiresAt`) MUST be checked before making API calls; expired sessions
  MUST redirect to login without showing a generic error.
- The token MUST be cleared on explicit logout and on any `401` response.
- Passwords MUST never be stored, logged, or passed beyond the login form submission.

### VI. Role-Based UI Behaviour

The UI MUST adapt based on the authenticated user's `role` (`"Customer"`, `"Agent"`,
`"Admin"`). Role checks are purely presentational — the server enforces real authorization.

- Route access MUST be guarded by a `<RoleGuard>` or equivalent wrapper that redirects
  unauthorised users to an appropriate page (e.g. their dashboard) without revealing that
  the route exists.
- Controls that map to forbidden actions (e.g. "Edit" for a Customer, "Reopen" for an
  Agent) MUST be hidden — not merely disabled — to avoid confusing users.
- Internal comments (`isInternal: true`) MUST NEVER be displayed to Customers; the API
  already filters them, but the frontend MUST treat the absence of the field as the norm.
- The "Reopen" action MUST only appear for Customers on their own Closed tickets, and
  only when within the 14-day window the server enforces.

### VII. Consistent UI States

Every data-fetching interaction MUST render all four states:

| State | Requirement |
|-------|-------------|
| **Loading** | Skeleton loaders or spinner; never blank white screen |
| **Error** | Friendly message + the API `detail` string; offer retry where applicable |
| **Empty** | Descriptive empty state (not a blank list); include a call to action if applicable |
| **Success** | The actual data; no toast for initial load; toast for mutations |

- Mutation outcomes (create, update, close, assign, delete) MUST show a success toast and
  trigger a data refetch.
- The `title` error code from ProblemDetails MUST map to user-friendly copy; unknown codes
  fall back to the `detail` string.
- Loading states MUST use content-aware skeletons (matching the shape of the expected
  content) rather than a generic spinner whenever feasible.

### VIII. Form Validation & Submission Hygiene

All forms MUST use a single, consistent validation library (e.g. `react-hook-form` +
`zod`). Inline ad-hoc validation in components is forbidden.

- Client-side validation is a UX convenience only; the server's response is authoritative.
  A `400 Validation.Failed` from the server MUST be surfaced to the user even if
  client-side validation passed.
- Every form field MUST have an associated `<label>` and an `aria-describedby` pointing
  to its error message element.
- Submit buttons MUST be disabled (and show a loading indicator) while a request is in
  flight to prevent duplicate submissions.
- File upload forms MUST enforce the allowed MIME types and 10 MB limit client-side as a
  first-pass UX guard (the server enforces them authoritatively).

### IX. Accessibility (a11y)

The application MUST meet WCAG 2.1 AA as a baseline.

- Every interactive element MUST be keyboard-reachable and have a visible focus ring.
- Color MUST NOT be the sole means of conveying information (e.g. SLA status badges MUST
  include a text label, not just a color).
- Dynamic content updates (toasts, modal openings, status changes) MUST use ARIA live
  regions or focus management so screen-reader users are informed.
- All images and icons that convey meaning MUST have `alt` text or `aria-label`;
  decorative icons MUST have `aria-hidden="true"`.
- Modals and drawers MUST trap focus while open and restore it on close.

### X. Responsive Design

The application MUST be fully usable on screens from 375 px (mobile) to 1440 px+ (desktop)
without horizontal scrolling or overlapping elements.

- Layout MUST use CSS Grid or Flexbox; hardcoded pixel widths for containers are forbidden.
- The ticket list, forms, and comment threads are the primary responsive concerns and MUST
  be tested at mobile, tablet, and desktop breakpoints.
- Touch targets MUST be at minimum 44 × 44 CSS pixels.

### XI. Testability & Documentation

Code MUST be written in a way that supports automated testing without large refactors.

- Business-adjacent logic (role checks, pagination helpers, form schemas, enum mappings)
  MUST live in pure functions in `lib/` or `hooks/`, not embedded in components.
- Every public function exported from `lib/` MUST have a JSDoc comment describing its
  inputs, outputs, and side-effects.
- Each feature folder MUST include a `README.md` documenting: the feature's purpose, the
  API endpoints it consumes, the roles that can access it, and notable business rules the
  UI reflects.
- Component `props` MUST be documented with JSDoc or inline TypeScript types that are
  self-descriptive.

---

## Technology Stack & Constraints

**Locked choices** (MUST NOT be changed without a constitution amendment):

| Concern | Choice |
|---------|--------|
| Framework | Next.js (App Router) |
| Language | TypeScript 5+, `strict: true` |
| Styling | Vanilla CSS / CSS Modules (no Tailwind unless amended) |
| Validation | `zod` for schema definition; `react-hook-form` for form state |
| HTTP client | Custom typed wrapper around `fetch` in `src/lib/api/` |
| Auth storage | Decision MUST be documented in `src/lib/auth/README.md` before first commit |

**Backend contract:**
- Base URL and auth scheme are defined in `FRONTEND_API_REFERENCE.md`.
- All enums are serialised as strings by the server (`JsonStringEnumConverter` is active).
- The server returns ISO 8601 UTC datetimes; the frontend MUST format them in the user's
  local timezone for display.

**Prohibited patterns:**
- No direct DOM manipulation (use React state and refs appropriately).
- No business logic in Next.js API routes unless building a BFF layer (which requires a
  constitution amendment).
- No hardcoded user IDs, role strings, or API base URLs outside designated config files.

---

## Development Workflow & Quality Gates

**Before merging any pull request, ALL of the following MUST pass:**

1. `tsc --noEmit` — zero TypeScript errors.
2. ESLint with the project ruleset — zero errors (warnings are tracked but do not block).
3. Prettier formatting check — no diffs.
4. Unit tests for any new or modified `lib/` or `hooks/` code.
5. Manual verification of all four UI states (loading, error, empty, success) for any
   new data-fetching screen.

**Naming conventions:**

| Artifact | Convention |
|----------|-----------|
| Components | `PascalCase.tsx` |
| Hooks | `useCamelCase.ts` |
| API modules | `camelCase.api.ts` |
| Types/interfaces | `PascalCase` prefixed with `I` only for interfaces that extend external contracts |
| Feature folders | `kebab-case/` |

**API Reference as a living document:**
`FRONTEND_API_REFERENCE.md` MUST be kept in sync with the backend. When the backend
changes, the reference MUST be updated before the frontend work begins. The reference is
not a substitute for reading the backend source — it is a derived summary.

---

## Governance

This constitution supersedes all other coding conventions, style guides, and architectural
decisions for the HelpDisk Next.js frontend. Where it conflicts with a third-party
library's documentation, this constitution wins unless technically impossible.

**Amendment procedure:**
1. Open a pull request that modifies only `.specify/memory/constitution.md`.
2. The PR description MUST state: the principle being changed, the reason, and the
   migration impact on existing code.
3. MAJOR amendments (removing or redefining a principle) require explicit team approval
   before merging.
4. MINOR amendments (new principle or materially expanded guidance) require at least one
   reviewer approval.
5. PATCH amendments (clarifications, wording) may be self-approved.
6. `LAST_AMENDED_DATE` MUST be updated to the merge date.
7. `CONSTITUTION_VERSION` MUST be incremented per semantic versioning:
   - MAJOR: backward-incompatible principle removal or redefinition.
   - MINOR: new principle or section added.
   - PATCH: clarification, wording, or typo fix.

**Compliance:**
- All PRs MUST include a checklist item: "I have read the constitution and this PR
  complies with all applicable principles."
- Violations discovered post-merge are addressed by the next PR touching the same code —
  no dedicated "fix PR" is required unless the violation is security-related.

**Runtime guidance:**
For feature-level implementation guidance, consult `.specify/memory/` spec and plan
files generated by `/speckit-specify` and `/speckit-plan`.

**Version**: 1.0.0 | **Ratified**: 2026-08-24 | **Last Amended**: 2026-08-24
