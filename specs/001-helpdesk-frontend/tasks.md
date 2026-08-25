# Implementation Tasks: HelpDisk Next.js Frontend

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Initialize Next.js App Router project with TypeScript and Tailwind CSS in `frontend/helpdesk-web`
- [ ] T002 [P] Configure ESLint and Prettier per constitution rules in `frontend/helpdesk-web/.eslintrc.json`
- [ ] T003 Create feature-based folder structure (`src/features`, `src/components`, `src/lib`, `src/hooks`, `src/types`)
- [ ] T004 [P] Copy all API data models to `src/types/index.ts` (UserSession, Ticket, Comment, etc.)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Implement centralized typed API client (fetch wrapper) with ProblemDetails error handling in `src/lib/api/client.ts`
- [ ] T006 [P] Implement authentication storage utilities (HTTP-only cookies/Server Actions) in `src/lib/auth.ts`
- [ ] T007 [P] Create `RoleGuard` component for route protection in `src/components/RoleGuard.tsx`
- [ ] T008 [P] Build shared base UI components (Button, Input, FormField, Modal) in `src/components/`
- [ ] T009 [P] Create generic UI state wrappers (Loading, Error, Empty, Success states) in `src/components/`
- [ ] T010 Setup basic root layout and navigation frame (Navbar, Sidebar) in `src/app/layout.tsx`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Authentication (Priority: P1) 🏆 MVP

**Goal**: Visitors can register as Customers or log in as any role, and are routed to their role-appropriate dashboard.

**Independent Test**: Open app unauthenticated, verify redirect to `/login`. Register Customer, verify redirect to `/dashboard`. Log in as Agent/Admin, verify specific dashboards. Log out.

### Implementation for User Story 1

- [ ] T011 [P] [US1] Create Auth feature API client functions in `src/features/auth/api.ts` (login, register, getCompanies)
- [ ] T012 [P] [US1] Define Zod validation schemas for login and registration forms in `src/features/auth/schemas.ts`
- [ ] T013 [US1] Build LoginForm component using react-hook-form in `src/features/auth/components/LoginForm.tsx`
- [ ] T014 [US1] Build RegisterForm component (with dynamic company dropdown) in `src/features/auth/components/RegisterForm.tsx`
- [ ] T015 [US1] Implement `/login` page in `src/app/login/page.tsx`
- [ ] T016 [US1] Implement `/register` page in `src/app/register/page.tsx`
- [ ] T017 [US1] Implement `/dashboard` layout and role-based redirect logic in `src/app/dashboard/page.tsx`
- [ ] T018 [US1] Implement session expiry/401 handling globally in `src/lib/api/client.ts`

**Checkpoint**: Authentication flows work. App handles sessions securely.

---

## Phase 4: User Story 2 - Customer: Create and Track Tickets (Priority: P1)

**Goal**: Customers can submit new tickets, view their own tickets in a list, and see ticket details/SLA.

**Independent Test**: Log in as Customer. Create ticket. View in list. Open details and verify fields.

### Implementation for User Story 2

- [ ] T019 [P] [US2] Create Tickets API client functions (create, getCustomerTickets, getTicketDetails) in `src/features/tickets/api.ts`
- [ ] T020 [P] [US2] Define Zod schema for Create Ticket form in `src/features/tickets/schemas.ts`
- [ ] T021 [US2] Build CreateTicketForm component in `src/features/tickets/components/CreateTicketForm.tsx`
- [ ] T022 [US2] Implement `/tickets/new` page in `src/app/tickets/new/page.tsx`
- [ ] T023 [US2] Build TicketList table/card component with sorting/filtering in `src/features/tickets/components/TicketList.tsx`
- [ ] T024 [US2] Implement `/tickets` page (Customer view) in `src/app/tickets/page.tsx`
- [ ] T025 [US2] Build TicketDetail view component (read-only info + SLA badge) in `src/features/tickets/components/TicketDetail.tsx`
- [ ] T026 [US2] Implement `/tickets/[id]` page in `src/app/tickets/[id]/page.tsx`

**Checkpoint**: Core Customer ticket lifecycle is functional.

---

## Phase 5: User Story 3 - Agent: Work on Tickets (Priority: P1)

**Goal**: Agents can view all tickets, assign, edit, close tickets, and add comments.

**Independent Test**: Log in as Agent. View full list. Assign unassigned ticket. Add public/internal comment. Close ticket.

### Implementation for User Story 3

- [ ] T027 [P] [US3] Add Agent-specific API functions (getAllTickets, assignTicket, updateTicket, closeTicket) to `src/features/tickets/api.ts`
- [ ] T028 [P] [US3] Create Comments API client functions in `src/features/comments/api.ts`
- [ ] T029 [US3] Update TicketList to support Agent/Admin view (all tickets, assignee column) in `src/features/tickets/components/TicketList.tsx`
- [ ] T030 [US3] Build TicketActionPanel (assign, edit, close controls) in `src/features/tickets/components/TicketActionPanel.tsx`
- [ ] T031 [US3] Build CommentList and AddCommentForm in `src/features/comments/components/`
- [ ] T032 [US3] Integrate ActionPanel and Comments into `/tickets/[id]` page, handling role permissions

**Checkpoint**: Core Agent workflow is complete. Both sides of the ticket support desk are operational.

---

## Phase 6: User Story 4 - Customer: Reopen a Closed Ticket (Priority: P2)

**Goal**: Customers can reopen their own tickets closed within 14 days.

**Independent Test**: Customer clicks Reopen on eligible closed ticket.

### Implementation for User Story 4

- [ ] T033 [P] [US4] Add reopenTicket API function to `src/features/tickets/api.ts`
- [ ] T034 [US4] Add Reopen button logic to Customer ticket detail view in `src/features/tickets/components/TicketDetail.tsx`

---

## Phase 7: User Story 5 - Attachments (Priority: P2)

**Goal**: Authenticated users can upload, download, and delete attachments on open tickets.

**Independent Test**: Upload file (check limits/types). Download file. Delete file.

### Implementation for User Story 5

- [ ] T035 [P] [US5] Create Attachments API client functions in `src/features/attachments/api.ts`
- [ ] T036 [US5] Build AttachmentList and UploadZone components in `src/features/attachments/components/`
- [ ] T037 [US5] Implement client-side validation for MIME types and 10MB limit in UploadZone
- [ ] T038 [US5] Integrate Attachments into `/tickets/[id]` page

---

## Phase 8: User Story 6 & 7 - Admin: Manage Agents & Categories (Priority: P2)

**Goal**: Admins can CRUD agents and categories.

**Independent Test**: Admin creates/edits Agent. Admin creates/edits/deletes Category.

### Implementation for User Story 6 & 7

- [ ] T039 [P] [US6] Create Agents API client functions in `src/features/agents/api.ts`
- [ ] T040 [P] [US7] Create Categories API client functions in `src/features/categories/api.ts`
- [ ] T041 [US6] Implement `/admin/agents` page with list and forms in `src/app/admin/agents/page.tsx`
- [ ] T042 [US7] Implement `/admin/categories` page with list and forms in `src/app/admin/categories/page.tsx`

---

## Phase 9: User Story 8 - Admin: Dashboard Reports (Priority: P3)

**Goal**: Admins can view KPI reporting dashboard.

**Independent Test**: Admin views `/admin/reports` with data charts/cards.

### Implementation for User Story 8

- [ ] T043 [P] [US8] Create Reports API client functions in `src/features/reports/api.ts`
- [ ] T044 [US8] Build KPI Cards and Chart components in `src/features/reports/components/`
- [ ] T045 [US8] Implement `/admin/reports` page in `src/app/admin/reports/page.tsx`

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T046 [P] Ensure all data-fetching views use the loading/error/empty/success wrappers
- [ ] T047 Perform full accessibility audit (keyboard nav, ARIA live regions for toasts)
- [ ] T048 Verify responsive layouts across mobile (375px), tablet, and desktop breakpoints
- [ ] T049 Write feature READMEs in `src/features/*/README.md`
- [ ] T050 Run `quickstart.md` validation scenarios

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1.
- **User Stories (Phase 3+)**: All depend on Foundational phase completion.
  - US1 (Auth) must be completed before any meaningful end-to-end testing of other stories can happen.
  - US2 (Customer Tickets) and US3 (Agent Tickets) should happen sequentially to build the core domain.
- **Polish (Final Phase)**: Depends on all desired user stories being complete.

### Implementation Strategy

#### MVP First (User Story 1, 2, 3)

1. Complete Phase 1 & 2 (Setup & Foundational).
2. Complete Phase 3 (Auth).
3. Complete Phase 4 (Customer Tickets).
4. Complete Phase 5 (Agent Tickets).
5. **STOP and VALIDATE**: Core helpdesk loop works.

#### Incremental Delivery
Following MVP, deliver US4, US5, US6/7, and US8 iteratively.
