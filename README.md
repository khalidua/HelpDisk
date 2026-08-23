# HelpDisk

A complete, runnable .NET solution built to **teach Clean Architecture and DDD**.

It implements a support-ticket system far enough to exercise every pattern across multiple features (tickets, categories, agents, attachments, reports, authentication) so you can see the same pattern repeat rather than infer it from a single example.

Every file carries comments explaining *why* it is shaped the way it is. Read the code, not just this page.

---

## The one rule

**Dependencies point inward. Nothing points out.**

```
             ┌─────────────────────────────────────────┐
             │              HelpDisk.API               │
             │  controllers, HTTP, Swagger, wiring     │
             └───────────────┬─────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        ▼                    │                    ▼
┌───────────────────────┐    │    ┌──────────────────────────────┐
│ HelpDisk.Application  │◄───┼────│   HelpDisk.Infrastructure    │
│ services, DTOs,       │    │    │   EF Core, repositories,     │
│ validators, use cases │    │    │   interceptors, migrations   │
└───────────┬───────────┘    │    └──────────────────────────────┘
            │                │
            ▼                ▼
        ┌─────────────────────────────────┐
        │        HelpDisk.Domain          │
        │  entities, aggregates, rules    │
        │  ── no references at all ──     │
        └─────────────────────────────────┘
```

| Project | References | Knows about |
|---|---|---|
| `HelpDisk.Domain` | **nothing** | business rules, and nothing else |
| `HelpDisk.Application` | Domain | use cases, DTOs, validation |
| `HelpDisk.Infrastructure` | Application | EF Core, SQL Server, JWT |
| `HelpDisk.API` | Application, Infrastructure | HTTP, JSON, status codes |

Note the two arrows into Application. **Infrastructure depends on Application, not the other way round.** Application declares interfaces (`ITicketRepository` lives in Domain, `ICurrentUser` in Application) and Infrastructure implements them. That inversion is the whole trick — see `docs/architecture.md`.

---

## Running it

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — required for Option A (recommended)
- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later — only required for Option B (`dotnet --version`)
- `dotnet-ef` tools — only required if you want to manage EF Core migrations manually (`dotnet tool install --global dotnet-ef`)

### Option A — Docker Compose (recommended, cross-platform)

```bash
# 1. Clone the repository
git clone https://github.com/cloud4rain-c4r/HelpDisk
cd HelpDisk

# 2. Start everything (SQL Server + API)
docker compose up --build
```

This starts:
- **SQL Server 2022** on port `1433`
- **HelpDisk API** on port `8081` → <http://localhost:8081/index.html>

The API container sets `ASPNETCORE_ENVIRONMENT=Development`, so migrations run automatically on startup. No manual database setup required.

### Option B — LocalDB (Windows / Visual Studio)

```bash
# 1. Clone and restore
git clone https://github.com/cloud4rain-c4r/HelpDisk
cd HelpDisk

# 2. Run the API (migrations are applied automatically on first start)
dotnet run --project src/HelpDisk.API
```

Then open **<https://localhost:7132>** — Swagger UI is served at the root.

On startup in Development the app applies any pending migrations, creating the `HelpDisk` database automatically. You do not need to run any database commands first.

---

## Try it in this order

The API is protected with JWT. Authenticate first, then use the token for all subsequent requests.

**1. Register a user**

```http
POST /api/auth/register
{ "email": "user@example.com", "password": "Password123!" }
```

**2. Log in and copy the token**

```http
POST /api/auth/login
{ "email": "user@example.com", "password": "Password123!" }
```

Click **Authorize** in Swagger UI and paste the returned token.

**3. Create a category**

```http
POST /api/categories
{ "name": "Hardware" }
```
Copy the returned `id`.

**4. Create a ticket** (uses the category id from step 3)

```http
POST /api/tickets
{
  "title": "Printer jammed",
  "description": "3rd floor",
  "priority": "High",
  "categoryId": "<the id>"
}
```

**5. Register an agent**

```http
POST /api/agents
{ "name": "Jane Smith", "email": "jane@example.com" }
```
Copy the agent `id`.

**6. Assign the ticket**

```http
PUT /api/tickets/{id}/assign
{ "assigneeId": "<agent id>" }
```
Watch the console for the domain event being dispatched.

**7. Close the ticket**

```http
PUT /api/tickets/{id}/close
```

**8. Try to assign the closed ticket** — expect **409 Conflict**

```http
PUT /api/tickets/{id}/assign
{ "assigneeId": "<agent id>" }
```

Step 8 is the one to dwell on. That refusal comes from `Ticket.Assign` in the Domain project — a class that has never heard of HTTP, EF Core, or the number 409.

---

## Follow one request all the way down

`POST /api/tickets`:

```
TicketsController.Create(CreateTicketRequest)                   [API]
  │  binds JSON, calls the service, nothing else
  ▼
ITicketService.CreateAsync(request, ct)                         [Application]
  │
  ├─ IValidator<CreateTicketRequest>.ValidateAsync              → is the request well-formed?
  │                                                               fails → Error.Validation → 400
  ├─ ICategoryRepository.ExistsAsync(request.CategoryId)        → does the category exist?
  │                                                               fails → CategoryErrors.NotFound → 404
  ├─ Ticket.Create(title, description, priority,                [Domain]
  │                categoryId, _currentUser.UserId)             → is this a legal Ticket?
  │      │                                                        fails → TicketErrors.* → 400
  │      └─ RaiseDomainEvent(TicketCreatedDomainEvent)
  │
  ├─ ITicketRepository.AddAsync(ticket)                         [Domain contract]
  │      └─ TicketRepository → _context.Tickets.AddAsync        [Infrastructure]
  │
  └─ IUnitOfWork.SaveChangesAsync()
         ├─ SoftDeleteInterceptor       (before SQL)  deletes → updates
         ├─ AuditableEntityInterceptor  (before SQL)  stamps CreatedOnUtc
         │  ─── the INSERT runs ───
         └─ DomainEventsInterceptor     (after SQL)   dispatches the event
  ▼
ApiController.HandleResult(Result<Guid>)                        [API]
     Error.Type → HTTP status. This is the ONLY place that mentions 404.
```

Notice that the word "404" appears exactly once in that trace, at the very last step. Domain said *not found*; the API translated.

---

## Where to start reading

In this order:

1. **`Domain/Tickets/Ticket.cs`** — the aggregate root. Everything else exists to serve this file.
2. **`Domain/Shared/Result.cs`** — why expected failures are values instead of exceptions.
3. **`Application/Features/Tickets/TicketService.cs`** — orchestration: the five beats every use case follows.
4. **`API/Abstractions/ApiController.cs`** — the seam where domain errors become HTTP.
5. **`Infrastructure/Persistence/Configurations/TicketConfiguration.cs`** — what a rich aggregate costs when it meets an ORM.
6. **`docs/architecture.md`** — the reasoning behind all of it, including where this template deliberately departs from common practice.

Then open **`docs/adding-a-feature.md`** and add a feature yourself.

---

## Running the tests

The solution includes two test projects:

```bash
# Run all tests
dotnet test

# Run only domain tests
dotnet test tests/HelpDisk.Domain.Tests

# Run only application-layer tests
dotnet test tests/HelpDisk.Application.Tests
```

**`HelpDisk.Domain.Tests`** — pure unit tests with no mocks, no in-memory database, no `WebApplicationFactory`. Microseconds to run. Possible only because `Ticket` depends on nothing:

```csharp
[Fact]
public void Assign_OnClosedTicket_Fails()
{
    var ticket = Ticket.Create("Printer jammed", "3rd floor",
                               TicketPriority.High, Guid.NewGuid(), "user-1").Value;
    ticket.Close();

    var result = ticket.Assign("agent-7");

    Assert.True(result.IsFailure);
    Assert.Equal(TicketErrors.CannotAssignClosedTicket, result.Error);
}
```

No mocks. No in-memory database. No fixture.

**`HelpDisk.Application.Tests`** — service-layer tests that mock repositories with Moq and verify orchestration logic in `TicketService`.

---

## What this solution contains

| Feature | Endpoints |
|---|---|
| Authentication | `POST /api/auth/register`, `POST /api/auth/login` |
| Tickets | Full CRUD + assign, close, reopen, add comment, SLA status |
| Categories | `GET`, `POST`, `DELETE /api/categories` |
| Agents | `GET`, `POST`, `DELETE /api/agents` |
| Attachments | `POST /api/tickets/{id}/attachments`, `DELETE` |
| Reports | `GET /api/reports/summary` |

All ticket endpoints require a valid JWT Bearer token. Pass the token returned by `/api/auth/login` in the `Authorization: Bearer <token>` header, or use the **Authorize** button in Swagger UI.

---

## What this template deliberately leaves out

No CQRS/MediatR, no CI pipelines, no logging framework, no localization, no API versioning, no caching, no background jobs, no message bus.

Every one of those is a real concern in a real system. They are absent because each would add machinery that obscures the thing being taught. `docs/architecture.md` explains what adding each one would involve and — more usefully — which layers would have to change.

The short answer, and the point of the whole exercise: **almost none of them touch Domain.**
