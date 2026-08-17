# HelpDisk --- Implemented Features

> Track completed HelpDisk backend features here. Add a new section
> whenever a feature is completed.

------------------------------------------------------------------------

## 1. Authentication & Identity

**Status:** ✅ Completed

### What we added

-   Customer registration.
-   Login with email/password.
-   JWT authentication.
-   Identity roles: `Customer`, `Agent`, `Admin`.
-   Initial Identity seeding for roles and the admin account.
-   `GET /api/Auth/me` for the authenticated user's information.

### Main folders edited

``` text
src/
├── HelpDisk.API/
│   ├── Controllers/AuthController.cs
│   ├── Services/CurrentUser.cs
│   └── Extensions/MigrationExtensions.cs
├── HelpDisk.Application/
│   ├── Abstractions/ICurrentUser.cs
│   ├── Abstractions/IIdentityService.cs
│   └── Features/Auth/
└── HelpDisk.Infrastructure/
    └── Identity/
        ├── IdentityService.cs
        ├── IdentitySeeder.cs
        └── AppUser.cs
```

------------------------------------------------------------------------

## 2. Current User

**Status:** ✅ Completed

### What we added

`ICurrentUser` exposes: - `UserId` - `UserName` - `Role`

`CurrentUser` reads these values from the authenticated JWT claims
through `HttpContext.User`.

``` text
JWT → HttpContext.User → CurrentUser → ICurrentUser → Application
```

### Main folders edited

``` text
src/
├── HelpDisk.Application/Abstractions/ICurrentUser.cs
└── HelpDisk.API/
    ├── Services/CurrentUser.cs
    └── Program.cs
```

------------------------------------------------------------------------

## 3. Ticket Authorization

**Status:** ✅ Completed

### What we added

The ticket controller requires authentication with `[Authorize]`.

Role restrictions: \| Operation \| Allowed \| \|---\|---\| \| Create \|
Authenticated users \| \| Search \| Authenticated users + ownership
filtering \| \| GetById \| Authenticated users + ownership filtering \|
\| Update \| Agent, Admin \| \| Assign \| Agent, Admin \| \| Close \|
Agent, Admin \| \| Reopen \| Owning Customer \| \| Add Comment \|
Access-controlled \| \| Delete \| Admin \|

### Main folder edited

``` text
src/HelpDisk.API/Controllers/TicketsController.cs
```

------------------------------------------------------------------------

## 4. Customer Ticket Ownership

**Status:** ✅ Completed

### What we added

Customers can only access their own tickets for: - Search. - Get by
ID. - Comments. - Reopening.

Agents/Admins can access tickets according to their role.

Unauthorized access to another customer's ticket returns `NotFound` to
avoid revealing whether the ticket exists.

### Main folders edited

``` text
src/
├── HelpDisk.Application/
│   └── Features/Tickets/TicketService.cs
└── HelpDisk.API/
    └── Services/CurrentUser.cs
```

------------------------------------------------------------------------

## 5. Ticket Reopening Rules

**Status:** ✅ Completed

### What we added

A closed ticket: - Can only be reopened by its owning Customer. - Can
only be reopened within 14 days of closing. - Cannot be reopened if it
is not closed.

Ownership is checked in the Application service. The ticket state and
14-day rule are enforced by the Domain aggregate.

### Main folders edited

``` text
src/
├── HelpDisk.Domain/Tickets/
│   ├── Ticket.cs
│   └── TicketErrors.cs
└── HelpDisk.Application/
    └── Features/Tickets/TicketService.cs
```

------------------------------------------------------------------------

## 6. Ticket Comments Authorization

**Status:** 🟡 Partially completed

### What we added

-   Customers can comment only on their own tickets.
-   Agents/Admins can comment on tickets they can access.
-   The Domain prevents comments on closed tickets.

### Main folders edited

``` text
src/
├── HelpDisk.Application/Features/Tickets/TicketService.cs
└── HelpDisk.Domain/Tickets/
    ├── Ticket.cs
    └── TicketErrors.cs
```

### Still to implement

-   Internal comments.
-   Comment listing with authorization.
-   Internal comments must be absent from Customer responses.

------------------------------------------------------------------------

## 7. Ticket Assignment Validation

**Status:** ✅ Completed

### What we added

Two separate checks are now used:

**Who performs the assignment?**

``` text
[Authorize(Roles = "Agent,Admin")]
```

**Who is being assigned?**

``` text
Assignee ID
    ↓
IIdentityService
    ↓
Find user + role
    ↓
Agent/Admin → allowed
Customer    → rejected
```

This prevents tickets from being assigned to Customers.

### Main folders edited

``` text
src/
├── HelpDisk.API/Controllers/TicketsController.cs
├── HelpDisk.Application/
│   ├── Abstractions/IIdentityService.cs
│   ├── Features/Auth/AuthErrors.cs
│   └── Features/Tickets/TicketService.cs
└── HelpDisk.Infrastructure/Identity/IdentityService.cs
```

------------------------------------------------------------------------

## 8. Identity User Lookup

**Status:** ✅ Completed

### What we added

`IIdentityService` can retrieve a user by ID and return their role.

This keeps `UserManager<AppUser>` inside Infrastructure.

``` text
Application
    ↓
IIdentityService
    ↓
Infrastructure
    ↓
UserManager<AppUser>
```

### Main folders edited

``` text
src/
├── HelpDisk.Application/
│   ├── Abstractions/IIdentityService.cs
│   └── Features/Auth/AuthErrors.cs
└── HelpDisk.Infrastructure/
    └── Identity/IdentityService.cs
```

------------------------------------------------------------------------

# Next Features

These are required by the project description and are not yet completed:

-   [ ] Internal comments
-   [ ] Comment listing with authorization
-   [ ] Attachments
    -   [ ] Upload
    -   [ ] Download
    -   [ ] Delete
    -   [ ] 10 MB limit
    -   [ ] Maximum 5 attachments per ticket
    -   [ ] File-type restrictions
    -   [ ] Authorization
-   [ ] Categories
    -   [ ] List categories
    -   [ ] Create category (Admin)
    -   [ ] Edit category (Admin)
    -   [ ] Response-time targets
-   [ ] SLA / response deadlines
    -   [ ] Calculate deadline from category
    -   [ ] Automatic SLA breach detection
-   [ ] Reporting
    -   [ ] Open tickets per agent
    -   [ ] Average resolution time per category
    -   [ ] Monthly SLA breaches
-   [ ] Admin agent-account management

------------------------------------------------------------------------

# Current Authorization Model

``` text
Customer
├── Create tickets
├── View own tickets
├── Comment on own tickets
├── Reopen own closed tickets within 14 days
└── Cannot assign, change priority/status, or close

Agent
├── View tickets
├── Assign tickets
├── Change priority/status
├── Close tickets
├── Comment
└── Write internal comments

Admin
├── Everything an Agent can do
├── Manage agents
├── Manage categories
├── Configure response-time targets
└── Reporting
```

------------------------------------------------------------------------

# Architecture Notes

The implementation follows this separation:

``` text
Controller
    ↓
HTTP / role authorization
    ↓
Application Service
    ↓
Use-case / ownership authorization
    ↓
Domain
    ↓
Business invariants
    ↓
Infrastructure
    ↓
Database / Identity
```

Keep: - HTTP concerns in API. - Use-case and access logic in
Application. - Business rules in Domain. - EF Core and ASP.NET Identity
details in Infrastructure.
