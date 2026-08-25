# Feature Specification: HelpDisk Next.js Frontend

**Feature Branch**: `001-helpdesk-frontend`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Create a Next.js frontend for the existing HelpDisk ASP.NET Core API
supporting Customer, Agent, and Admin roles."

## Clarifications

### Session 2026-08-24

- Q: Since the registration form must let Customers pick a company but the backend has no documented endpoint to list companies — should the company list be fetched from a backend endpoint that needs to be added, or should the companies be hardcoded/pre-populated on the frontend for now? → A: Add a `GET /api/companies` endpoint to the backend and fetch the list dynamically on the registration page.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 – Authentication (Priority: P1)

Any visitor can register as a Customer or log in as any role. The system routes each user
to their role-appropriate dashboard and protects all pages behind authentication.

**Why this priority**: Every other story depends on a valid, identified session. Without
authentication the application cannot be used at all.

**Independent Test**: Open the app unauthenticated, verify redirection to login.
Register a new Customer account, log in, and verify arrival at the Customer dashboard.
Log in as an Agent and verify the Agent dashboard is shown. Log in as Admin and verify
the Admin dashboard.

**Acceptance Scenarios**:

1. **Given** a visitor who is not logged in, **When** they navigate to any protected page,
   **Then** they are redirected to the login page.
2. **Given** a new user, **When** they complete the registration form with a valid email,
   password, first name, last name, and a valid company selection, **Then** their account
   is created and they are logged in as a Customer.
3. **Given** a registered user, **When** they submit the login form with correct credentials,
   **Then** they receive a session token and are redirected to their role-specific dashboard.
4. **Given** a logged-in user, **When** they click Log Out, **Then** their session is
   cleared and they are redirected to the login page.
5. **Given** a user with an expired session, **When** they perform any action, **Then**
   they are silently redirected to the login page without a confusing error.
6. **Given** a Customer navigating directly to an Admin-only URL, **When** the page loads,
   **Then** they are redirected to their own dashboard — the Admin page is not revealed.

---

### User Story 2 – Customer: Create and Track Tickets (Priority: P1)

A logged-in Customer can submit a new support ticket, view their own tickets in a
searchable list, and track each ticket's status and SLA information.

**Why this priority**: This is the primary value the application delivers to Customers —
without it the frontend provides no meaningful function for the largest user group.

**Independent Test**: Log in as a Customer, create a ticket, verify it appears in the
Customer's ticket list, open the detail page, and verify all fields are displayed.

**Acceptance Scenarios**:

1. **Given** a logged-in Customer, **When** they submit a new ticket with a title,
   description, priority, and category, **Then** the ticket appears in their list with
   status "New".
2. **Given** a logged-in Customer, **When** they view their ticket list, **Then** they
   only see tickets they personally reported — no other customers' tickets appear.
3. **Given** a logged-in Customer, **When** they open a ticket detail, **Then** they see
   the ticket number, title, description, status, priority, category, SLA deadline,
   SLA status, comments (public only), and attachments.
4. **Given** a logged-in Customer, **When** they filter their ticket list by status,
   priority, or keyword, **Then** the list updates to match the filter criteria.
5. **Given** a ticket whose SLA deadline has passed before any agent response,
   **When** the Customer views the ticket, **Then** the SLA status shows "Breached"
   with a visible indicator.

---

### User Story 3 – Agent: Work on Tickets (Priority: P1)

A logged-in Agent can view all tickets across all customers, assign tickets to themselves
or other agents, update ticket details, close tickets, and add both public and internal
comments.

**Why this priority**: Agents are the primary users doing the work the system is designed
to support. Their experience must function independently to validate core workflow.

**Independent Test**: Log in as an Agent, view the full ticket list, open an unassigned
ticket, assign it, add an internal comment, add a public comment, and close it.

**Acceptance Scenarios**:

1. **Given** a logged-in Agent, **When** they view the ticket list, **Then** all tickets
   from all customers are visible (not scoped to a company).
2. **Given** a logged-in Agent, **When** they assign an open ticket to an agent,
   **Then** the ticket status changes to "In Progress" and the assignee is recorded.
3. **Given** a logged-in Agent, **When** they update the title, description, or priority
   of a non-closed ticket, **Then** the changes are saved.
4. **Given** a logged-in Agent, **When** they close a ticket, **Then** the ticket status
   changes to "Closed" and no further edits, assignments, or comments are possible.
5. **Given** a logged-in Agent, **When** they post a comment with "Internal" marked,
   **Then** the comment is visible to Agents and Admins only — it does not appear when
   a Customer views the same ticket.
6. **Given** a logged-in Agent, **When** they post the first comment on a ticket,
   **Then** the ticket's SLA status is evaluated and displays either "Met" or "Breached".
7. **Given** a closed ticket, **When** an Agent attempts to assign, edit, or comment,
   **Then** the action is rejected with an explanation that the ticket must be reopened first.

---

### User Story 4 – Customer: Reopen a Closed Ticket (Priority: P2)

A Customer who reported a ticket can reopen it after it has been closed, provided no
more than 14 days have passed since closing.

**Why this priority**: Important for customer satisfaction but less critical than initial
ticket creation and the core agent workflow.

**Independent Test**: Log in as the Customer who created a recently-closed ticket,
click Reopen, and verify the ticket returns to an open status.

**Acceptance Scenarios**:

1. **Given** a Customer viewing one of their own closed tickets closed within 14 days,
   **When** they click "Reopen", **Then** the ticket status returns to "New" or
   "In Progress" (depending on whether it was previously assigned) and they can
   add comments again.
2. **Given** a Customer viewing a closed ticket that was closed more than 14 days ago,
   **When** they view the detail, **Then** the "Reopen" button is not shown.
3. **Given** a Customer viewing someone else's closed ticket (which they cannot reach),
   **Then** the ticket is not visible — no Reopen action is possible.

---

### User Story 5 – Attachments (Priority: P2)

Any authenticated user can upload attachments to an open ticket they have access to,
download any attachment on a visible ticket, and delete their own attachments.

**Why this priority**: Enhances ticket quality but the core ticket workflow functions
without it.

**Independent Test**: Open a ticket detail, upload a file, verify it appears in the
attachment list, download it, then delete it.

**Acceptance Scenarios**:

1. **Given** an open ticket, **When** a user uploads a file within the allowed types
   (JPEG, PNG, PDF, TXT, ZIP, DOCX, XLSX) and under 10 MB, **Then** the file appears
   in the attachment list with name, type, size, and upload time.
2. **Given** an attachment on a visible ticket, **When** a user clicks "Download",
   **Then** the file downloads with its original name.
3. **Given** a closed ticket, **When** a user attempts to upload an attachment,
   **Then** the upload is rejected with a message that the ticket must be reopened first.
4. **Given** a ticket that already has 5 attachments, **When** a user attempts to
   upload another, **Then** the upload is rejected with a message that the limit
   has been reached.
5. **Given** a file larger than 10 MB or of a disallowed type, **When** a user selects
   it for upload, **Then** the upload is rejected with a clear explanation.

---

### User Story 6 – Admin: Manage Agents (Priority: P2)

An Admin can create agent accounts, update agent profile information, and
activate or deactivate agents.

**Why this priority**: Essential for system administration but not critical to the
end-user support experience.

**Independent Test**: Log in as Admin, create a new agent, verify it appears in the
agent list, deactivate it, and verify the status reflects the change.

**Acceptance Scenarios**:

1. **Given** the Admin agent management page, **When** the Admin submits a new agent form
   with email, password, first name, and last name, **Then** the agent account is created
   with the "Agent" role and appears in the list.
2. **Given** an existing agent, **When** the Admin updates the agent's name or email,
   **Then** the changes are reflected immediately.
3. **Given** an active agent, **When** the Admin clicks "Deactivate", **Then** the
   agent's status changes to inactive.
4. **Given** an inactive agent, **When** the Admin clicks "Activate", **Then** the
   agent's status changes to active.

---

### User Story 7 – Admin: Manage Categories (Priority: P2)

An Admin can create, rename, and delete ticket categories. Each category has an SLA
response time target.

**Why this priority**: Categories are required for ticket creation, so this admin function
is a prerequisite dependency — but existing seeded categories mean it is not day-one
critical.

**Independent Test**: Log in as Admin, create a category with a name and SLA hours,
verify it appears in the list and in the Create Ticket form, then delete it.

**Acceptance Scenarios**:

1. **Given** the Admin category management page, **When** the Admin creates a category
   with a unique name and a positive SLA hours value, **Then** the category appears in
   the list and becomes available in the ticket creation form.
2. **Given** a category name that already exists, **When** the Admin submits the create
   form, **Then** creation is rejected with a "name already exists" message.
3. **Given** a category with no tickets assigned, **When** the Admin deletes it,
   **Then** the category is removed from the list and the ticket creation dropdown.
4. **Given** a category that has tickets assigned, **When** the Admin attempts to
   delete it, **Then** deletion is rejected with an explanation.

---

### User Story 8 – Admin: Dashboard Reports (Priority: P3)

An Admin can view a reporting dashboard that shows open-ticket workload per agent,
average resolution time per category, and SLA breach count for the current month.

**Why this priority**: Valuable for oversight but the system fully functions for all
roles without it.

**Independent Test**: Log in as Admin, navigate to the reports dashboard, and verify
all three report panels display data from the backend.

**Acceptance Scenarios**:

1. **Given** the Admin reports page, **When** it loads, **Then** a chart or table shows
   the count of open tickets grouped by assigned agent (null group shown as "Unassigned").
2. **Given** the Admin reports page, **When** it loads, **Then** a chart or table shows
   average resolution time in hours per ticket category.
3. **Given** the Admin reports page, **When** it loads, **Then** a KPI card shows the
   total number of SLA breaches in the current calendar month.

---

### Edge Cases

- What happens when a Customer registers with a company ID that does not exist?
  → The form shows a server-returned error; the user must select a valid company.
- What happens when a Customer tries to view a ticket URL belonging to another customer?
  → They receive a "not found" page — no ticket information is disclosed.
- What happens when an Agent tries to assign a ticket to a user who is a Customer?
  → The assignment is rejected with a message that only Agents and Admins can be assigned.
- What happens when a Reopen attempt is made on a ticket closed more than 14 days ago?
  → The Reopen button is not rendered; if attempted via direct API the server rejects it.
- What happens when a user uploads a file of a disallowed MIME type?
  → A client-side check rejects it immediately; the server also enforces this.
- What happens when the network is unavailable during a form submission?
  → The form shows a network error state with a retry option; no data is lost.
- What happens when session token expires mid-session?
  → The next request returns 401; the user is redirected to login with a message.
- What happens when a category is updated while a ticket creation form is open?
  → The category list is refreshed on form open; stale selections are caught by the server.

---

## Requirements *(mandatory)*

### Functional Requirements

**Authentication & Sessions**

- **FR-001**: The system MUST allow any visitor to log in using an email address and password.
- **FR-002**: The system MUST allow new users to self-register as Customers, selecting from
  available companies retrieved dynamically via `GET /api/companies`.
- **FR-003**: The system MUST store the session token and the user's role for the duration
  of the authenticated session.
- **FR-004**: The system MUST redirect unauthenticated users to the login page when they
  access any protected route.
- **FR-005**: The system MUST redirect users with insufficient roles away from role-restricted
  pages without exposing that those pages exist.
- **FR-006**: The system MUST clear the session and redirect to login when the token expires
  or a 401 response is received.

**Ticket Management — All Roles**

- **FR-007**: The system MUST display tickets in a paginated, filterable list. Agents and
  Admins see all tickets; Customers see only their own.
- **FR-008**: The system MUST allow filtering the ticket list by keyword, status, priority,
  and category.
- **FR-009**: The system MUST allow sorting the ticket list by created date, priority, or
  status.
- **FR-010**: The system MUST display a full ticket detail page showing: ticket number,
  title, description, status, priority, category, reporter, assignee, creation date,
  SLA deadline, SLA status, comments, and attachments.
- **FR-011**: The system MUST allow any authenticated user to create a ticket with a title,
  description, priority, and category.
- **FR-012**: The system MUST apply field-level validation before submission; if the server
  returns validation errors they MUST also be surfaced to the user.

**Ticket Management — Agent & Admin**

- **FR-013**: The system MUST allow Agents and Admins to update a non-closed ticket's
  title, description, and priority.
- **FR-014**: The system MUST allow Agents and Admins to assign a non-closed ticket to
  any user with the Agent or Admin role.
- **FR-015**: The system MUST allow Agents and Admins to close any non-closed ticket.
- **FR-016**: The system MUST prevent Agents and Admins from editing, assigning, or
  commenting on a Closed ticket — the relevant controls MUST be hidden.

**Ticket Management — Customer**

- **FR-017**: The system MUST display a "Reopen" button on a Customer's own Closed tickets
  that were closed within the last 14 days.
- **FR-018**: The system MUST NOT display a "Reopen" button on Closed tickets older than
  14 days or on any ticket not belonging to the logged-in Customer.

**Comments**

- **FR-019**: The system MUST allow any authenticated user with access to the ticket to
  add a public comment to a non-closed ticket.
- **FR-020**: The system MUST allow Agents and Admins to mark a comment as "Internal".
  Internal comments MUST NOT be visible to Customers.
- **FR-021**: The system MUST display who posted each comment and when.
- **FR-022**: The system MUST prevent comment submission on Closed tickets.

**Attachments**

- **FR-023**: The system MUST allow any authenticated user with ticket access to upload
  attachments to a non-closed ticket.
- **FR-024**: Permitted file types are: JPEG, PNG, PDF, TXT, ZIP, DOCX, XLSX. The system
  MUST reject other types with a clear message, enforced client-side and server-side.
- **FR-025**: The system MUST reject files larger than 10 MB with a clear message.
- **FR-026**: The system MUST reject an upload when the ticket already has 5 attachments.
- **FR-027**: The system MUST allow any user with ticket access to download any attachment
  from a visible ticket.
- **FR-028**: The system MUST allow attachment deletion by any user with ticket access.
- **FR-029**: The system MUST prevent attachment upload to a Closed ticket.

**SLA Display**

- **FR-030**: The system MUST display the SLA response deadline on every ticket detail
  and in the ticket list.
- **FR-031**: The system MUST display the SLA status ("Pending", "Met", or "Breached")
  with a distinct visual indicator that does not rely on color alone.
- **FR-032**: Tickets with SLA status "Breached" MUST be visually distinguished in the
  ticket list.

**Agent Management (Admin only)**

- **FR-033**: The system MUST allow Admins to view a list of all agents with their
  name, email, and active status.
- **FR-034**: The system MUST allow Admins to create an agent account by providing
  email, password, first name, and last name.
- **FR-035**: The system MUST allow Admins to update an agent's name and email.
- **FR-036**: The system MUST allow Admins to deactivate and reactivate agent accounts.

**Category Management (Admin only)**

- **FR-037**: The system MUST allow Admins to view a list of all categories with their
  name and SLA response time target.
- **FR-038**: The system MUST allow Admins to create a category with a unique name and
  a positive SLA response time in hours.
- **FR-039**: The system MUST allow Admins to update a category's name and SLA hours.
- **FR-040**: The system MUST allow Admins to delete a category that has no associated
  tickets.
- **FR-041**: The system MUST reject deletion of a category that has associated tickets
  and display an explanation.

**Reports (Admin only)**

- **FR-042**: The system MUST display the count of open tickets grouped by assigned agent
  (unassigned tickets shown separately).
- **FR-043**: The system MUST display the average ticket resolution time in hours grouped
  by category.
- **FR-044**: The system MUST display the count of SLA breaches in the current
  calendar month.

**UI States**

- **FR-045**: Every data-fetching view MUST display a loading state, an error state with
  a retry option, an empty state with a call to action, and the populated data state.
- **FR-046**: Every mutation (create, update, close, assign, delete) MUST show a success
  notification and trigger a data refresh.

### Key Entities

- **User Session**: Authenticated identity including user ID, display name, role
  (`Customer`, `Agent`, `Admin`), and token expiry. Customers also carry a company ID.
- **Ticket**: Core record identified by a system-generated ticket number. Has title,
  description, priority, status, category, SLA deadline, SLA status, reporter, and
  optional assignee. Has sub-collections of comments and attachments.
- **Comment**: A text note on a ticket. May be internal (agent/admin only) or public.
  Carries author and timestamp.
- **Attachment**: A file linked to a ticket. Carries file name, type, size, uploader,
  and upload time. Up to 5 per ticket.
- **Category**: A ticket classification with a name and an SLA response time target in
  hours.
- **Agent**: A support staff member. Has email, name, and active status. No company
  affiliation.
- **Company**: An organisation that Customers belong to. Customers register under a
  specific company; tickets are company-isolated for Customer views.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new user can complete registration and submit their first support ticket
  in under 3 minutes from opening the application for the first time.
- **SC-002**: An Agent can triage, assign, and close a ticket in under 60 seconds from
  the ticket list view.
- **SC-003**: All four UI states (loading, error, empty, success) are present and
  correctly triggered for every data-fetching screen — verifiable by manual checklist.
- **SC-004**: Customers can access no ticket, comment, or attachment belonging to another
  company — zero cross-company data visible in any scenario tested.
- **SC-005**: Internal comments are never visible to Customers in any navigable route —
  zero internal comments exposed in Customer sessions during testing.
- **SC-006**: All role-restricted pages redirect correctly — no Admin or Agent page is
  accessible to a Customer, and no Admin-only page is accessible to an Agent.
- **SC-007**: The ticket list responds to filter and sort changes without a full-page
  reload, updating results visibly within 2 seconds on a standard connection.
- **SC-008**: Every interactive element is reachable and operable by keyboard alone —
  verifiable by navigating the full application without a mouse.
- **SC-009**: All form validation errors are surfaced alongside the relevant field, not
  only as a page-level alert — verified by submitting each form with invalid input.
- **SC-010**: The application renders correctly and is fully usable on screens from
  375 px (mobile) to 1440 px wide — verified by browser resize testing.

---

## Assumptions

- The HelpDisk ASP.NET Core backend is already deployed and accessible at a configurable
  base URL. The frontend does not need to stand up or manage the backend.
- The API contract documented in `FRONTEND_API_REFERENCE.md` is the authoritative source.
  Any backend behavior not documented there is treated as unknown and not relied upon.
- Registration is available only for the Customer role. Agent and Admin accounts are
  created by Admins through the management UI or pre-seeded.
- There is no "company management" screen in the frontend; companies are pre-seeded in
  the backend and retrieved for customer self-registration via a read-only `GET /api/companies` endpoint.
- The application does not support multi-language or internationalisation in this version;
  English is the only language.
- Email confirmation, password reset, and profile editing (beyond what the Admin can do
  for agents) are out of scope for this version.
- The frontend does not implement real-time push notifications; the ticket list and
  detail page can be manually refreshed.
- All dates and times received from the API (UTC) will be displayed in the user's local
  browser timezone.
- The seeded test accounts (`admin@helpdisk.com`, `agent1@helpdisk.com`, etc.) are
  available for development and testing; production accounts are managed by Admins.
- Mobile support (375 px+) is in scope for layout and usability; native mobile app
  features (push, offline) are out of scope.
