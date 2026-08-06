# HelpDisk

A small, complete, runnable .NET solution built to **teach Clean Architecture and DDD**.

It is not a product. It implements one feature (support tickets) far enough to exercise every pattern, plus a deliberately tiny second feature (categories) so you can see the pattern repeat rather than infer it from a single example.

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
| `HelpDisk.Infrastructure` | Application | EF Core, SQL Server |
| `HelpDisk.API` | Application, Infrastructure | HTTP, JSON, status codes |

Note the two arrows into Application. **Infrastructure depends on Application, not the other way round.** Application declares interfaces (`ITicketRepository` lives in Domain, `ICurrentUser` in Application) and Infrastructure implements them. That inversion is the whole trick — see `docs/architecture.md`.

---

## Running it

**Prerequisites**

- .NET SDK 10.0 or later — `dotnet --version`
- SQL Server LocalDB (ships with Visual Studio; also available standalone)
- `dotnet-ef` tools — `dotnet tool install --global dotnet-ef`

**Run**

```bash
dotnet run --project src/HelpDisk.API
```

Then open **<https://localhost:7132>** — Swagger UI is served at the root.

On startup in Development the app applies any pending migrations, creating the `HelpDisk` database if it does not exist. You do not need to run any database commands first.

**Not on Windows / no LocalDB?** Run SQL Server in Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_password123" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

and change `ConnectionStrings:Database` in `src/HelpDisk.API/appsettings.json` to:

```
Server=localhost,1433;Database=HelpDisk;User Id=sa;Password=Your_password123;TrustServerCertificate=True
```

**Try it in this order** — a ticket needs a category to exist first:

1. `POST /api/categories` → `{ "name": "Hardware" }` — copy the returned id
2. `POST /api/tickets` → `{ "title": "Printer jammed", "description": "3rd floor", "priority": "High", "categoryId": "<the id>" }`
3. `PUT /api/tickets/{id}/assign` → `{ "assigneeId": "agent-7" }` — watch the console for the domain event
4. `PUT /api/tickets/{id}/close`
5. `PUT /api/tickets/{id}/assign` again → **409**, because a closed ticket cannot be assigned

Step 5 is the one to dwell on. That refusal comes from `Ticket.Assign` in the Domain project — a class that has never heard of HTTP, EF Core, or the number 409.

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

## Testing the domain (there is no test project — on purpose)

This solution has exactly four projects, as specified. But the payoff of a rich domain model is testability, so here is what those tests look like:

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

No mocks. No in-memory database. No `WebApplicationFactory`. No fixture. Microseconds to run.

That is possible only because `Ticket` depends on nothing. The moment a domain class needs a repository or a `DbContext`, this test needs a mocking framework and a setup block, and people stop writing it.

To add tests: `dotnet new xunit -o tests/HelpDisk.Domain.Tests`, reference `HelpDisk.Domain`, and note that the test project needs **no other reference at all**.

---

## What this template deliberately leaves out

No CQRS/MediatR, no authentication, no CI pipelines, no logging framework, no localization, no API versioning, no caching, no background jobs, no message bus.

Every one of those is a real concern in a real system. They are absent because each would add machinery that obscures the thing being taught. `docs/architecture.md` explains what adding each one would involve and — more usefully — which layers would have to change.

The short answer, and the point of the whole exercise: **almost none of them touch Domain.**
