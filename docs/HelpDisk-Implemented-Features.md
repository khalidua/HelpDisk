# HelpDisk --- Implemented Features

> Track completed HelpDisk backend features here. Add a new section whenever a feature is completed.

------------------------------------------------------------------------

## 1. Authentication & Identity

**Status:** ✅ Completed

### What we added

- Customer registration with Email, Password, FirstName, LastName, and CompanyId.
- Login with email/password.
- JWT authentication.
- Identity roles: `Customer`, `Agent`, `Admin`.
- Initial Identity seeding for roles and the admin account.
- `GET /api/Auth/me` for the authenticated user's information.
- Company membership is stored on `AppUser` through `CompanyId`.
- Customer registration verifies that the provided company exists.

### Main folders edited

```text
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
        ├── AppUser.cs
        └── JwtTokenProvider.cs
```

------------------------------------------------------------------------

## 2. Current User & Company Context

**Status:** ✅ Completed

### What we added

`ICurrentUser` exposes:

- `UserId`
- `UserName`
- `Role`
- `CompanyId`

The authenticated user's information flows through the JWT:

```text
AppUser.CompanyId
       ↓
UserInfo.CompanyId
       ↓
JWT claim
       ↓
HttpContext.User
       ↓
CurrentUser.CompanyId
       ↓
ICurrentUser
       ↓
Application
```

This keeps ASP.NET Core request details out of the Application layer.

### Main folders edited

```text
src/
├── HelpDisk.Application/
│   ├── Abstractions/ICurrentUser.cs
│   └── Features/Auth/
├── HelpDisk.API/
│   ├── Services/CurrentUser.cs
│   └── Program.cs
└── HelpDisk.Infrastructure/
    └── Identity/
        ├── AppUser.cs
        ├── IdentityService.cs
        └── JwtTokenProvider.cs
```

------------------------------------------------------------------------

## 3. Company & User Membership

**Status:** ✅ Completed

### What we added

- `Company` domain entity.
- EF Core `CompanyConfiguration`.
- `CompanyId` on `AppUser`.
- `AppUserConfiguration` linking users to companies.
- EF Core migration for the company relationship.
- Customers are assigned to a company during registration.

The relationship is:

```text
Company
   │
   │ 1
   │
   └──────────< AppUser
                  │
                  └── CompanyId
```

Deleting a company sets the users' `CompanyId` to `NULL` instead of deleting the users.

### Main folders edited

```text
src/
├── HelpDisk.Domain/
│   └── Companies/
│       └── Company.cs
└── HelpDisk.Infrastructure/
    └── Persistence/
        ├── Configurations/
        │   ├── CompanyConfiguration.cs
        │   └── AppUserConfiguration.cs
        └── Migrations/
```

------------------------------------------------------------------------

## 4. Ticket Authorization

**Status:** ✅ Completed

### What we added

The ticket controller requires authentication with `[Authorize]`.

| Operation | Allowed |
|---|---|
| Create | Authenticated users |
| Search | Authenticated users + customer ownership/company filtering |
| GetById | Authenticated users + customer ownership/company filtering |
| Update | Agent, Admin |
| Assign | Agent, Admin |
| Close | Agent, Admin |
| Reopen | Owning Customer |
| Add Comment | Access-controlled |
| Get Comments | Access-controlled |
| Delete | Admin |

### Main folder edited

```text
src/
└── HelpDisk.API/
    └── Controllers/
        └── TicketsController.cs
```

------------------------------------------------------------------------

## 5. Customer Ticket Ownership & Company Isolation

**Status:** ✅ Completed

### What we added

Customers are restricted to their own tickets while also enforcing their company boundary.

For customer access, both conditions must be satisfied:

```text
Ticket.ReporterId == CurrentUser.UserId
                    AND
Reporter.CompanyId == CurrentUser.CompanyId
```

This protection is applied to:

- Ticket search.
- Getting a ticket by ID.
- Adding comments.
- Viewing comments.
- Reopening a ticket.

Customers cannot choose a `CompanyId` through the ticket search request. The company comes from the authenticated user's identity/JWT.

We deliberately **did not add `CompanyId` to `Ticket`**. The company is determined through:

```text
Ticket.ReporterId
       ↓
AppUser
       ↓
AppUser.CompanyId
```

Unauthorized access to another customer's or another company's ticket returns `NotFound` rather than revealing whether the ticket exists.

### Main folders edited

```text
src/
├── HelpDisk.Application/
│   ├── Abstractions/ICurrentUser.cs
│   └── Features/Tickets/
│       ├── TicketService.cs
│       └── Dtos/
├── HelpDisk.Domain/
│   └── Repositories/
│       └── ITicketRepository.cs
└── HelpDisk.Infrastructure/
    ├── Identity/
    └── Persistence/
        └── Repositories/
            └── TicketRepository.cs
```

------------------------------------------------------------------------

## 6. Ticket Reopening Rules

**Status:** ✅ Completed

### What we added

A closed ticket:

- Can only be reopened by its owning Customer.
- Can only be reopened within 14 days of closing.
- Cannot be reopened if it is not closed.

Ownership and company access are checked in the Application service.

The ticket state and 14-day rule are enforced by the Domain aggregate.

### Main folders edited

```text
src/
├── HelpDisk.Domain/Tickets/
│   ├── Ticket.cs
│   └── TicketErrors.cs
└── HelpDisk.Application/
    └── Features/Tickets/
        └── TicketService.cs
```

------------------------------------------------------------------------

## 7. Ticket Comments Authorization

**Status:** ✅ Completed

### What we added

- Customers can comment only on their own accessible tickets.
- Customer access also requires the reporter to belong to the customer's company.
- Agents/Admins can comment according to their access.
- Customers cannot create internal comments.
- Customers cannot see internal comments.
- Comment listing is protected by ticket ownership/company authorization.
- The Domain prevents comments on closed tickets.

### Main folders edited

```text
src/
├── HelpDisk.Application/
│   └── Features/Tickets/
│       └── TicketService.cs
└── HelpDisk.Domain/Tickets/
    ├── Ticket.cs
    └── TicketErrors.cs
```

------------------------------------------------------------------------

## 8. Ticket Assignment Validation

**Status:** ✅ Completed

### What we added

Two separate checks are used.

**Who performs the assignment?**

```text
[Authorize(Roles = "Agent,Admin")]
```

**Who is being assigned?**

```text
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

```text
src/
├── HelpDisk.API/
│   └── Controllers/TicketsController.cs
├── HelpDisk.Application/
│   ├── Abstractions/IIdentityService.cs
│   ├── Features/Auth/AuthErrors.cs
│   └── Features/Tickets/TicketService.cs
└── HelpDisk.Infrastructure/
    └── Identity/IdentityService.cs
```

------------------------------------------------------------------------

## 9. Identity User Lookup

**Status:** ✅ Completed

### What we added

`IIdentityService` can retrieve a user by ID and return:

- User ID
- Email
- First name
- Last name
- Role
- Company ID

This keeps `UserManager<AppUser>` inside Infrastructure.

```text
Application
    ↓
IIdentityService
    ↓
Infrastructure
    ↓
UserManager<AppUser>
```

### Main folders edited

```text
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

- [ ] Attachments
  - [ ] Upload
  - [ ] Download
  - [ ] Delete
  - [ ] 10 MB limit
  - [ ] Maximum 5 attachments per ticket
  - [ ] File-type restrictions
  - [ ] Authorization

- [ ] Categories
  - [ ] List categories
  - [ ] Create category (Admin)
  - [ ] Edit category (Admin)
  - [ ] Response-time targets

- [ ] SLA / response deadlines
  - [ ] Calculate deadline from category
  - [ ] Automatic SLA breach detection

- [ ] Reporting
  - [ ] Open tickets per agent
  - [ ] Average resolution time per category
  - [ ] Monthly SLA breaches

- [ ] Admin agent-account management

------------------------------------------------------------------------

# Current Authorization Model

```text
Customer
├── Register with a CompanyId
├── Create tickets
├── View own tickets within their company
├── Comment on own tickets within their company
├── Cannot create internal comments
├── Cannot see internal comments
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

```text
Controller
    ↓
HTTP / role authorization
    ↓
Application Service
    ↓
Use-case / ownership / company authorization
    ↓
Domain
    ↓
Business invariants
    ↓
Infrastructure
    ↓
Database / Identity
```

Keep:

- HTTP concerns in API.
- Use-case and access logic in Application.
- Business rules and invariants in Domain.
- EF Core and ASP.NET Identity details in Infrastructure.
- Company membership on `AppUser`, rather than duplicating `CompanyId` on every ticket.
