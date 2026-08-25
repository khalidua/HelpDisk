# HelpDisk – Frontend API Reference

> **Source of truth:** ASP.NET Core backend at `src/HelpDisk.API`.  
> **Base URL (dev):** `http://localhost:5000` (or `https://localhost:7000`)  
> **Swagger UI (dev):** served at the root `/`  
> All request/response bodies are **JSON**. Enums are serialised as **strings** (e.g. `"High"`, `"New"`) — *not* integers.

---

## Table of Contents

1. [Authentication & JWT](#1-authentication--jwt)
2. [Roles](#2-roles)
3. [Error Response Format](#3-error-response-format)
4. [HTTP Status Code Map](#4-http-status-code-map)
5. [Auth Endpoints](#5-auth-endpoints)
6. [Ticket Endpoints](#6-ticket-endpoints)
7. [Comment Endpoints](#7-comment-endpoints)
8. [Attachment Endpoints](#8-attachment-endpoints)
9. [Category Endpoints](#9-category-endpoints)
10. [Agent Endpoints](#10-agent-endpoints)
11. [Report Endpoints](#11-report-endpoints)
12. [Enums Reference](#12-enums-reference)
13. [Ticket Lifecycle & Business Rules](#13-ticket-lifecycle--business-rules)
14. [Authorization Matrix](#14-authorization-matrix)
15. [Seeded Test Accounts](#15-seeded-test-accounts)

---

## 1. Authentication & JWT

The API uses **Bearer JWT** authentication.

- After login, the server returns a JWT in the `token` field of `TokenResponse`.
- Every protected endpoint requires the header:
  ```
  Authorization: Bearer <token>
  ```
- The token encodes: `UserId` (NameIdentifier claim), `UserName` (Name), `Email`, `Role`, and `CompanyId` (GroupSid).
- Default expiry: **60 minutes** (configurable in `appsettings.json → Jwt.ExpirationMinutes`).

**Frontend must:**
- Store the JWT (e.g. `localStorage` or an HTTP-only cookie).
- Read `role` from `TokenResponse.role` to drive UI routing — never decode the JWT payload for business logic.
- Handle `401` by redirecting to login.
- Handle token expiry by repeating login.

---

## 2. Roles

There are exactly **three roles**, case-sensitive:

| Role | Description |
|------|-------------|
| `"Customer"` | End-user who raises and follows up their own tickets. Belongs to a company. |
| `"Agent"` | Support staff who work on tickets. No company affiliation. |
| `"Admin"` | Full access. Can manage agents, categories, and delete tickets. |

> Agents **and** Admins can be assigned to tickets and perform all staff-side actions.

---

## 3. Error Response Format

All errors follow **RFC 7807 ProblemDetails**:

```json
{
  "type":   "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title":  "Ticket.CannotAssignClosed",
  "status": 409,
  "detail": "A closed ticket cannot be assigned. Reopen it first."
}
```

| Field | Frontend usage |
|-------|---------------|
| `title` | Stable machine-readable error **code** — key your UI logic on this |
| `detail` | Human-readable sentence — safe to display to the user |
| `status` | HTTP status code (redundant with HTTP status line) |

> ⚠️ **Branch on `title` (the error code), not on `detail`** — the description text may change; the code will not.

### Common error codes

| Code | HTTP | Meaning |
|------|------|---------|
| `Validation.Failed` | 400 | FluentValidation failure — `detail` contains all joined messages |
| `Ticket.NotFound` | 404 | Ticket does not exist (or customer cannot see it) |
| `Ticket.AlreadyClosed` | 409 | Ticket is already closed |
| `Ticket.NotClosed` | 409 | Cannot reopen — ticket is not closed |
| `Ticket.CannotReopenExpired` | 409 | Cannot reopen — more than 14 days since closing |
| `Ticket.CannotAssignClosed` | 409 | Cannot assign a closed ticket |
| `Ticket.CannotCommentOnClosed` | 409 | Cannot comment on a closed ticket |
| `Ticket.CannotEditClosed` | 409 | Cannot edit a closed ticket |
| `Ticket.InternalCommentNotAllowed` | 400 | Customer tried to post an internal comment |
| `Ticket.InvalidAssignee` | 400 | Assignee is not an Agent or Admin |
| `Ticket.ConcurrencyConflict` | 409 | Ticket modified by another user concurrently |
| `Tickets.MaximumAttachmentsReached` | 409 | Ticket already has 5 attachments |
| `Tickets.CannotAddAttachmentToClosedTicket` | 409 | Cannot attach to a closed ticket |
| `Attachments.FileTooLarge` | 400 | File exceeds 10 MB |
| `Attachments.FileTypeNotAllowed` | 400 | File MIME type is not permitted |
| `Attachments.NotFound` | 404 | Attachment not found |
| `Attachments.TicketNotFound` | 404 | Ticket not found (attachment scope) |
| `Category.NotFound` | 404 | Category does not exist |
| `Category.NameAlreadyExists` | 409 | Category name is taken |
| `Category.CannotDeleteWithTickets` | 409 | Category has attached tickets |
| `Agents.NotFound` | 404 | Agent does not exist |
| `Agents.CreationFailed` | 400 | Agent account creation failed (Identity error) |
| `Agents.UpdateFailed` | 400 | Agent account update failed |
| `Agents.ActivationFailed` | 400 | Could not activate agent |
| `Agents.DeactivationFailed` | 400 | Could not deactivate agent |
| `Auth.InvalidCredentials` | 400 | Wrong email or password |
| `Auth.RegistrationFailed` | 400 | User could not be registered (Identity error) |
| `Auth.CompanyNotFound` | 404 | Company ID supplied in register does not exist |

---

## 4. HTTP Status Code Map

| HTTP | Meaning |
|------|---------|
| `200 OK` | Success (with body for GET/POST, empty body for PUT/DELETE) |
| `400 Bad Request` | Validation failure or invalid input |
| `401 Unauthorized` | No/invalid JWT |
| `403 Forbidden` | Valid JWT but insufficient role |
| `404 Not Found` | Resource does not exist *or* customer cannot see it (intentional — no info leakage) |
| `409 Conflict` | Request is valid but the resource is in the wrong state |
| `500 Internal Server Error` | Unexpected exception (bug or outage) |

---

## 5. Auth Endpoints

### 5.1 Register (Customer self-registration)

```
POST /api/auth/register
```

**Auth required:** No  
**Roles:** —

**Request body:**
```json
{
  "email":     "user@example.com",
  "password":  "Secret123!",
  "firstName": "Jordan",
  "lastName":  "Lee",
  "companyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

| Field | Type | Notes |
|-------|------|-------|
| `email` | string | Must be unique |
| `password` | string | ASP.NET Identity rules apply (e.g. ≥6 chars, mixed case, digit, symbol) |
| `firstName` | string | |
| `lastName` | string | |
| `companyId` | Guid | Must reference an existing company |

**Response `200 OK`:** `string` — the new user's ID (opaque string from ASP.NET Identity)

**Important errors:**
- `400 Auth.RegistrationFailed` — email already taken or password too weak
- `404 Auth.CompanyNotFound` — company ID not found

**Frontend feature:** Register page / sign-up flow for Customers.

---

### 5.2 Login

```
POST /api/auth/login
```

**Auth required:** No  
**Roles:** —

**Request body:**
```json
{
  "email":    "admin@helpdisk.com",
  "password": "Admin123!"
}
```

**Response `200 OK`:**
```json
{
  "token":     "<JWT string>",
  "expiresAt": "2026-08-24T15:40:00Z",
  "role":      "Admin"
}
```

| Field | Type | Notes |
|-------|------|-------|
| `token` | string | JWT Bearer token — store and attach to all subsequent requests |
| `expiresAt` | ISO 8601 datetime (UTC) | Schedule token refresh or re-login before this time |
| `role` | `"Customer"` \| `"Agent"` \| `"Admin"` | Use to gate UI routes |

**Important errors:**
- `400 Auth.InvalidCredentials` — wrong email or password

**Frontend feature:** Login page. After login, read `role` and redirect to the appropriate dashboard.

---

## 6. Ticket Endpoints

All ticket endpoints require a valid JWT. Additional role restrictions are noted per endpoint.

---

### 6.1 Search / List Tickets

```
GET /api/tickets
```

**Auth required:** Yes  
**Roles:** All authenticated (Customer, Agent, Admin)

**Query parameters (all optional):**

| Param | Type | Default | Notes |
|-------|------|---------|-------|
| `keyword` | string | — | Full-text search on title |
| `status` | `"New"` \| `"InProgress"` \| `"Closed"` | — | Filter by status |
| `priority` | `"Low"` \| `"Normal"` \| `"High"` \| `"Urgent"` | — | Filter by priority |
| `categoryId` | Guid | — | Filter by category |
| `assigneeId` | string | — | Filter by assignee user ID |
| `fromDate` | ISO 8601 datetime | — | Created on or after |
| `toDate` | ISO 8601 datetime | — | Created on or before |
| `sortBy` | `"CreatedOn"` \| `"Priority"` \| `"Status"` | — | Sort field (case-insensitive) |
| `descending` | bool | `true` | Sort direction |
| `page` | int | `1` | Must be ≥ 1 |
| `pageSize` | int | `20` | 1–100 (max enforced server-side) |

> **Customer isolation:** Customers only see tickets they personally reported. The `reporterId` and `companyId` filters are applied server-side automatically — do not send them from the frontend.

**Response `200 OK`:**
```json
{
  "data": [
    {
      "id":                  "guid",
      "ticketNumber":        "HD-001",
      "title":               "Laptop screen flickering",
      "status":              "New",
      "priority":            "High",
      "categoryId":          "guid",
      "assigneeId":          null,
      "responseDeadlineUtc": "2026-08-24T18:00:00Z",
      "slaStatus":           "Pending",
      "createdOnUtc":        "2026-08-24T14:00:00Z"
    }
  ],
  "currentPage":    1,
  "pageSize":       20,
  "totalPages":     3,
  "totalItems":     50,
  "hasPreviousPage": false,
  "hasNextPage":    true
}
```

**Note:** List items omit `description` and `comments` — those are only in the full detail response.

**Important errors:**
- `400 Validation.Failed` — invalid `page`/`pageSize`/`sortBy`/enum values

**Frontend feature:** Ticket list / dashboard, search & filter UI.

---

### 6.2 Get Ticket by ID (Full Detail)

```
GET /api/tickets/{ticketId}
```

**Auth required:** Yes  
**Roles:** All authenticated

**Path params:**
- `ticketId` — Guid

**Response `200 OK`:**
```json
{
  "id":                  "guid",
  "ticketNumber":        "HD-001",
  "title":               "Laptop screen flickering",
  "description":         "My laptop screen starts flickering...",
  "status":              "New",
  "priority":            "High",
  "categoryId":          "guid",
  "reporterId":          "user-id-string",
  "assigneeId":          null,
  "createdOnUtc":        "2026-08-24T14:00:00Z",
  "modifiedOnUtc":       null,
  "closedOnUtc":         null,
  "responseDeadlineUtc": "2026-08-24T18:00:00Z",
  "slaStatus":           "Pending",
  "comments": [
    {
      "id":           "guid",
      "body":         "We are looking into this.",
      "authorId":     "agent-user-id",
      "createdOnUtc": "2026-08-24T14:30:00Z",
      "isInternal":   false
    }
  ],
  "attachments": [
    {
      "id":           "guid",
      "fileName":     "screenshot.png",
      "contentType":  "image/png",
      "fileSize":     204800,
      "uploadedById": "user-id-string",
      "createdOnUtc": "2026-08-24T14:05:00Z"
    }
  ]
}
```

> **Customer isolation:** A Customer receives `404` if the ticket belongs to a different user or company — no information leak.  
> **Internal comments:** `isInternal: true` comments are **filtered out** before sending to Customers.

**Important errors:**
- `404 Ticket.NotFound`

**Frontend feature:** Ticket detail page.

---

### 6.3 Create Ticket

```
POST /api/tickets
```

**Auth required:** Yes  
**Roles:** All authenticated (Customer, Agent, Admin)

**Request body:**
```json
{
  "title":       "Printer not working",
  "description": "The 3rd floor printer is offline after the update.",
  "priority":    "Normal",
  "categoryId":  "guid"
}
```

| Field | Type | Constraints |
|-------|------|-------------|
| `title` | string | Required, max 200 chars |
| `description` | string | Required, max 4,000 chars |
| `priority` | string enum | `"Low"`, `"Normal"`, `"High"`, `"Urgent"` |
| `categoryId` | Guid | Must reference an existing category |

> **Reporter is never sent by the client.** The server reads `UserId` from the JWT claim.

**Response `200 OK`:** `"guid"` — the new ticket's ID (a JSON string).

**Important errors:**
- `400 Validation.Failed` — missing title/description, invalid priority enum
- `404 Category.NotFound` — `categoryId` not found

**Frontend feature:** New Ticket form (available to all roles).

---

### 6.4 Update Ticket (Title / Description / Priority)

```
PUT /api/tickets/{ticketId}
```

**Auth required:** Yes  
**Roles:** `Agent`, `Admin`

**Request body:**
```json
{
  "title":       "Updated title",
  "description": "Updated description.",
  "priority":    "High"
}
```

| Field | Type | Constraints |
|-------|------|-------------|
| `title` | string | Required, max 200 chars |
| `description` | string | Required, max 4,000 chars |
| `priority` | string enum | `"Low"`, `"Normal"`, `"High"`, `"Urgent"` |

**Response `200 OK`:** Empty body.

**Important errors:**
- `404 Ticket.NotFound`
- `409 Ticket.CannotEditClosed` — ticket is closed; reopen first

**Frontend feature:** Edit ticket form on ticket detail page (shown only to Agent/Admin).

---

### 6.5 Assign Ticket

```
PUT /api/tickets/{ticketId}/assign
```

**Auth required:** Yes  
**Roles:** `Agent`, `Admin`

**Request body:**
```json
{
  "assigneeId": "agent-user-id-string"
}
```

> Assignee must be a user with role `"Agent"` or `"Admin"`. The server validates this.  
> Assigning moves the ticket status from `New` → `InProgress` if it is not already.

**Response `200 OK`:** Empty body.

**Important errors:**
- `404 Ticket.NotFound`
- `400 Ticket.InvalidAssignee` — target user is a Customer
- `409 Ticket.CannotAssignClosed` — ticket is closed

**Frontend feature:** "Assign to agent" control on ticket detail (Agent/Admin view).

---

### 6.6 Close Ticket

```
PUT /api/tickets/{ticketId}/close
```

**Auth required:** Yes  
**Roles:** `Agent`, `Admin`

**Request body:** None

**Response `200 OK`:** Empty body.

**Important errors:**
- `404 Ticket.NotFound`
- `409 Ticket.AlreadyClosed`

**Frontend feature:** "Close ticket" button on ticket detail (Agent/Admin view).

---

### 6.7 Reopen Ticket

```
PUT /api/tickets/{ticketId}/reopen
```

**Auth required:** Yes  
**Roles:** `Customer` **only**

> Only the Customer who reported the ticket (and belongs to the same company) can reopen it.  
> The ticket must have been closed within the last **14 days**.

**Request body:** None

**Response `200 OK`:** Empty body.

**Important errors:**
- `404 Ticket.NotFound` — ticket not found *or* caller is not the reporter/same company
- `409 Ticket.NotClosed` — ticket is not currently closed
- `409 Ticket.CannotReopenExpired` — closed more than 14 days ago

**Frontend feature:** "Reopen" button on ticket detail (Customer view, closed tickets only, within 14 days).

---

### 6.8 Delete Ticket (Soft Delete)

```
DELETE /api/tickets/{ticketId}
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

> Soft delete — the row is flagged `IsDeleted = true` in the database and excluded from all queries.

**Request body:** None

**Response `200 OK`:** Empty body.

**Important errors:**
- `404 Ticket.NotFound`

**Frontend feature:** "Delete ticket" action in Admin view.

---

## 7. Comment Endpoints

Comments are **nested under tickets** — there is no independent `/api/comments` endpoint.

---

### 7.1 Get Comments

```
GET /api/tickets/{ticketId}/comments
```

**Auth required:** Yes  
**Roles:** All authenticated

> **Customer isolation:** Same ticket-ownership check as `GET /api/tickets/{ticketId}`.  
> **Internal comments:** Comments with `isInternal: true` are **stripped** before the response reaches a Customer.

**Response `200 OK`:**
```json
[
  {
    "id":           "guid",
    "body":         "We are investigating.",
    "authorId":     "agent-user-id",
    "createdOnUtc": "2026-08-24T14:30:00Z",
    "isInternal":   false
  }
]
```

**Important errors:**
- `404 Ticket.NotFound`

**Frontend feature:** Comment thread on ticket detail page.

---

### 7.2 Add Comment

```
POST /api/tickets/{ticketId}/comments
```

**Auth required:** Yes  
**Roles:** All authenticated

**Request body:**
```json
{
  "body":       "Please send the error log.",
  "isInternal": false
}
```

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `body` | string | — | Required, max 2,000 chars |
| `isInternal` | bool | `false` | Customers **cannot** set this to `true` |

> **Author is never sent by the client** — read from JWT.  
> **SLA side-effect:** When an Agent or Admin posts the **first** comment, the server evaluates the SLA: if `utcNow ≤ responseDeadlineUtc` → `slaStatus = "Met"`, otherwise `slaStatus = "Breached"`.

**Response `200 OK`:** `"guid"` — the new comment's ID.

**Important errors:**
- `404 Ticket.NotFound`
- `409 Ticket.CannotCommentOnClosed` — ticket is closed
- `400 Ticket.InternalCommentNotAllowed` — Customer set `isInternal: true`
- `400 Validation.Failed` — empty body or too long

**Frontend feature:** Comment input box on ticket detail page.

---

## 8. Attachment Endpoints

Routes are **nested under tickets**: `/api/tickets/{ticketId}/attachments`.

---

### 8.1 Upload Attachment

```
POST /api/tickets/{ticketId}/attachments
```

**Auth required:** Yes  
**Roles:** All authenticated

**Request:** `multipart/form-data`

| Field | Notes |
|-------|-------|
| `file` | The binary file. Required; must not be empty. |

**Server-side constraints:**
- Max file size: **10 MB**
- Max attachments per ticket: **5**
- Allowed MIME types: `image/jpeg`, `image/png`, `application/pdf`, `text/plain`, `application/zip`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document` (`.docx`), `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` (`.xlsx`)

> **Customer isolation:** Same ticket-ownership check as comments.  
> Cannot upload to a **closed** ticket.

**Response `200 OK`:** `"guid"` — the new attachment's ID.

**Important errors:**
- `400` (plain string) — `"A file is required."` (controller-level guard, no error code)
- `404 Attachments.TicketNotFound`
- `409 Tickets.CannotAddAttachmentToClosedTicket`
- `409 Attachments.MaximumAttachmentsReached` (or `Tickets.MaximumAttachmentsReached`)
- `400 Attachments.FileTooLarge`
- `400 Attachments.FileTypeNotAllowed`
- `400 Attachments.FileNameInvalid`

**Frontend feature:** File upload widget on ticket detail page.

---

### 8.2 Download Attachment

```
GET /api/tickets/{ticketId}/attachments/{attachmentId}
```

**Auth required:** Yes  
**Roles:** All authenticated

> **Customer isolation:** Same ticket-ownership check.

**Response `200 OK`:** Binary file stream with:
- `Content-Type` header set to the stored `contentType`
- `Content-Disposition: attachment; filename="<fileName>"` header

> This is a direct file download — not a JSON body. Use `window.open(url)` or an `<a href>` with the `Authorization` header via a `fetch` → `Blob` approach.

**Important errors:**
- `404 Attachments.TicketNotFound`
- `404 Attachments.NotFound`

**Frontend feature:** "Download" link next to each attachment in ticket detail.

---

### 8.3 Delete Attachment

```
DELETE /api/tickets/{ticketId}/attachments/{attachmentId}
```

**Auth required:** Yes  
**Roles:** All authenticated

> **Customer isolation:** Same ticket-ownership check.

**Response `200 OK`:** Empty body.

**Important errors:**
- `404 Attachments.TicketNotFound`
- `404 Attachments.NotFound`

**Frontend feature:** "Remove" button on attachment list (ticket detail).

---

## 9. Category Endpoints

---

### 9.1 List All Categories

```
GET /api/categories
```

**Auth required:** Yes  
**Roles:** All authenticated

**Response `200 OK`:**
```json
[
  {
    "id":                     "guid",
    "name":                   "Hardware",
    "responseTimeTargetHours": 4,
    "createdOnUtc":           "2026-01-01T00:00:00Z"
  }
]
```

> Categories are returned sorted **alphabetically** by name.  
> `responseTimeTargetHours` is the SLA target used when a ticket is created in this category.

**Frontend feature:** Category dropdown in Create Ticket form; Category management page (Admin).

---

### 9.2 Create Category

```
POST /api/categories
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Request body:**
```json
{
  "name":                    "Security",
  "responseTimeTargetHours": 1
}
```

| Field | Type | Constraints |
|-------|------|-------------|
| `name` | string | Required; unique |
| `responseTimeTargetHours` | int | Required; must be > 0 |

**Response `200 OK`:** `"guid"` — the new category's ID.

**Important errors:**
- `400 Validation.Failed`
- `409 Category.NameAlreadyExists`

**Frontend feature:** "Add Category" form on Category management page.

---

### 9.3 Update Category

```
PUT /api/categories/{categoryId}
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Request body:**
```json
{
  "name":                    "Security Incidents",
  "responseTimeTargetHours": 2
}
```

**Response `200 OK`:** Empty body.

**Important errors:**
- `400 Validation.Failed`
- `404 Category.NotFound`
- `409 Category.NameAlreadyExists`

**Frontend feature:** "Edit Category" form on Category management page.

---

### 9.4 Delete Category

```
DELETE /api/categories/{categoryId}
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

> Cannot delete a category that has tickets assigned to it.

**Response `200 OK`:** Empty body.

**Important errors:**
- `404 Category.NotFound`
- `409 Category.CannotDeleteWithTickets`

**Frontend feature:** "Delete" button on Category management page.

---

## 10. Agent Endpoints

All agent endpoints require role `Admin`.

---

### 10.1 List All Agents

```
GET /api/agents
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Response `200 OK`:**
```json
[
  {
    "id":        "user-id-string",
    "email":     "agent1@helpdisk.com",
    "firstName": "Alex",
    "lastName":  "Morgan",
    "isActive":  true
  }
]
```

**Frontend feature:** Agent management page.

---

### 10.2 Get Agent by ID

```
GET /api/agents/{userId}
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Path params:**
- `userId` — string (ASP.NET Identity user ID)

**Response `200 OK`:** Same shape as a single item from List.

**Important errors:**
- `404 Agents.NotFound`

**Frontend feature:** Agent detail / edit page.

---

### 10.3 Create Agent

```
POST /api/agents
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Request body:**
```json
{
  "email":     "newagent@helpdisk.com",
  "password":  "Secret123!",
  "firstName": "Sam",
  "lastName":  "Rivera"
}
```

> Agents are created without a `companyId`. The server assigns the `"Agent"` role automatically.

**Response `200 OK`:** `"user-id-string"` — the new agent's ID.

**Important errors:**
- `400 Agents.CreationFailed` — Identity validation failed (email taken, weak password, etc.)

**Frontend feature:** "Add Agent" form on Agent management page.

---

### 10.4 Update Agent

```
PUT /api/agents/{userId}
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Request body:**
```json
{
  "email":     "updated@helpdisk.com",
  "firstName": "Alex",
  "lastName":  "Johnson"
}
```

> Password changes are not supported through this endpoint.

**Response `200 OK`:** Updated `UserInfo` object (same shape as AgentResponse).

**Important errors:**
- `404 Agents.NotFound`
- `400 Agents.UpdateFailed`

**Frontend feature:** "Edit Agent" form on Agent management page.

---

### 10.5 Deactivate Agent

```
POST /api/agents/{userId}/deactivate
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Request body:** None

> Sets `IsActive = false` on the user. Deactivated agents can still have tickets assigned to them historically, but cannot log in.

**Response `200 OK`:** Empty body.

**Important errors:**
- `404 Agents.NotFound`
- `400 Agents.DeactivationFailed`

**Frontend feature:** "Deactivate" toggle on Agent management page.

---

### 10.6 Activate Agent

```
POST /api/agents/{userId}/activate
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Request body:** None

**Response `200 OK`:** Empty body.

**Important errors:**
- `404 Agents.NotFound`
- `400 Agents.ActivationFailed`

**Frontend feature:** "Activate" toggle on Agent management page (for previously deactivated agents).

---

## 11. Report Endpoints

All report endpoints require role `Admin`.

---

### 11.1 Open Tickets Per Agent

```
GET /api/reports/opened-tickets-per-agent
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Response `200 OK`:**
```json
[
  {
    "agentId":         "user-id-string",
    "openTicketsCount": 5
  }
]
```

> `agentId` can be `null` for unassigned tickets.

**Frontend feature:** Admin dashboard workload chart / table.

---

### 11.2 Average Resolution Time Per Category

```
GET /api/reports/average-resolution-time-per-category
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Response `200 OK`:**
```json
[
  {
    "categoryId":                    "guid",
    "averageResolutionTimeInHours":  6.5
  }
]
```

**Frontend feature:** Admin dashboard performance chart.

---

### 11.3 SLA Breaches This Month

```
GET /api/reports/sla-breaches-this-month
```

**Auth required:** Yes  
**Roles:** `Admin` **only**

**Response `200 OK`:**
```json
{
  "breachCount": 3
}
```

> Counts tickets with `slaStatus = "Breached"` whose response was recorded in the current calendar month (UTC).

**Frontend feature:** Admin dashboard KPI card / summary stat.

---

## 12. Enums Reference

> Enums are sent and received as **strings**, not numbers.

### TicketStatus

| Value | Integer | Transitions |
|-------|---------|-------------|
| `"New"` | 1 | Initial state on creation; also set on Reopen (if no assignee) |
| `"InProgress"` | 2 | Set when a ticket is assigned; also set on Reopen (if already had an assignee) |
| `"Closed"` | 3 | Set by Close action |

### TicketPriority

| Value | Integer |
|-------|---------|
| `"Low"` | 1 |
| `"Normal"` | 2 |
| `"High"` | 3 |
| `"Urgent"` | 4 |

### TicketSlaStatus

| Value | Meaning |
|-------|---------|
| `"Pending"` | No agent response yet |
| `"Met"` | Agent first responded within the deadline |
| `"Breached"` | Agent first responded after the deadline |

---

## 13. Ticket Lifecycle & Business Rules

### State machine

```
         Assign (Agent/Admin)
New ─────────────────────────► InProgress
 │                                  │
 │     Close (Agent/Admin)           │  Close (Agent/Admin)
 └───────────────────────────────────┴──────────► Closed
                                                      │
                         Reopen (Customer, ≤14 days)  │
    ◄──────────────────────────────────────────────────┘
    (→ New if no assignee, → InProgress if assignee exists)
```

### Rules summary

| Rule | Who enforces | HTTP result |
|------|-------------|-------------|
| A ticket must have a title (≤ 200 chars) | Domain + Validator | `400` |
| A ticket must have a description (≤ 4,000 chars) | Domain + Validator | `400` |
| CategoryId must reference an existing category | Service | `404` |
| Reporter is taken from JWT, never from client | Service | — |
| Cannot edit a Closed ticket | Domain | `409` |
| Cannot assign a Closed ticket | Domain | `409` |
| Assignee must be Agent or Admin | Service | `400` |
| Cannot comment on a Closed ticket | Domain | `409` |
| Customers cannot post internal comments | Service | `400` |
| SLA resolved on first agent/admin comment | Service | — |
| Only the ticket's reporter (same company) can reopen | Service | `404` |
| Can only reopen within 14 days of closing | Domain | `409` |
| Max 5 attachments per ticket | Domain + Service | `409` |
| Cannot attach to Closed ticket | Domain | `409` |
| Max attachment size: 10 MB | Service | `400` |
| Allowed MIME types: jpg, png, pdf, txt, zip, docx, xlsx | Service | `400` |
| Delete is soft-delete (row remains, excluded from queries) | Infrastructure interceptor | — |
| Category with tickets cannot be deleted | Service | `409` |

### SLA mechanics

1. When a ticket is created, the server sets `responseDeadlineUtc = createdOnUtc + category.responseTimeTargetHours`.
2. Initial `slaStatus` is `"Pending"`.
3. When an Agent or Admin posts the **first** comment:
   - If `utcNow ≤ responseDeadlineUtc` → `slaStatus = "Met"`
   - If `utcNow > responseDeadlineUtc` → `slaStatus = "Breached"`
4. Once resolved (`"Met"` or `"Breached"`), SLA status cannot change again.

---

## 14. Authorization Matrix

| Endpoint | Customer | Agent | Admin |
|----------|:--------:|:-----:|:-----:|
| `POST /auth/register` | ✅ | ✅ | ✅ |
| `POST /auth/login` | ✅ | ✅ | ✅ |
| `GET /tickets` | ✅ (own only) | ✅ (all) | ✅ (all) |
| `GET /tickets/{id}` | ✅ (own only) | ✅ | ✅ |
| `POST /tickets` | ✅ | ✅ | ✅ |
| `PUT /tickets/{id}` | ❌ | ✅ | ✅ |
| `PUT /tickets/{id}/assign` | ❌ | ✅ | ✅ |
| `PUT /tickets/{id}/close` | ❌ | ✅ | ✅ |
| `PUT /tickets/{id}/reopen` | ✅ (own only) | ❌ | ❌ |
| `DELETE /tickets/{id}` | ❌ | ❌ | ✅ |
| `GET /tickets/{id}/comments` | ✅ (own, no internal) | ✅ | ✅ |
| `POST /tickets/{id}/comments` | ✅ (own, not internal) | ✅ | ✅ |
| `GET /tickets/{id}/attachments/{aid}` | ✅ (own only) | ✅ | ✅ |
| `POST /tickets/{id}/attachments` | ✅ (own only) | ✅ | ✅ |
| `DELETE /tickets/{id}/attachments/{aid}` | ✅ (own only) | ✅ | ✅ |
| `GET /categories` | ✅ | ✅ | ✅ |
| `POST /categories` | ❌ | ❌ | ✅ |
| `PUT /categories/{id}` | ❌ | ❌ | ✅ |
| `DELETE /categories/{id}` | ❌ | ❌ | ✅ |
| `GET /agents` | ❌ | ❌ | ✅ |
| `GET /agents/{id}` | ❌ | ❌ | ✅ |
| `POST /agents` | ❌ | ❌ | ✅ |
| `PUT /agents/{id}` | ❌ | ❌ | ✅ |
| `POST /agents/{id}/activate` | ❌ | ❌ | ✅ |
| `POST /agents/{id}/deactivate` | ❌ | ❌ | ✅ |
| `GET /reports/*` | ❌ | ❌ | ✅ |

---

## 15. Seeded Test Accounts

All passwords: **`Test123!`** (agents/customers) · **`Admin123!`** (admin)

| Email | Role | Company |
|-------|------|---------|
| `admin@helpdisk.com` | Admin | — |
| `agent1@helpdisk.com` | Agent | — |
| `agent2@helpdisk.com` | Agent | — |
| `customer1@helpdisk.com` | Customer | TechCorp |
| `customer2@helpdisk.com` | Customer | TechCorp |
| `customer3@helpdisk.com` | Customer | Retail Ltd |

### Seeded categories (SLA hours)

| Category | SLA Target |
|----------|-----------|
| Hardware | 4 h |
| Network | 2 h |
| Software | 8 h |
| Access Request | 24 h |
| Security | 1 h |
