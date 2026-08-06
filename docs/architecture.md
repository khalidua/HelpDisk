# Architecture

The reasoning behind HelpDisk's structure. `README.md` shows *what* it looks like; this explains *why*, including the places where it deliberately departs from what you will meet elsewhere.

---

## 1. Why four layers

The goal of Clean Architecture is that **business rules do not depend on details**. A detail is anything you might plausibly change: the database, the web framework, the message bus, the way you serialize JSON.

Turn that around: if changing SQL Server to PostgreSQL forces you to edit a class that describes when a ticket may be assigned, the two are tangled, and every future change to either will cost more than it should.

The four projects encode that separation as a compiler-enforced rule:

| Layer | Contains | Changes when |
|---|---|---|
| **Domain** | Entities, aggregates, business rules, error definitions, repository *interfaces* | the business changes |
| **Application** | Use cases, DTOs, validation, orchestration | a workflow changes |
| **Infrastructure** | EF Core, repository *implementations*, interceptors, migrations | a technology changes |
| **API** | Controllers, HTTP, Swagger, composition root | the transport changes |

The value is not the folder structure — it is that `HelpDisk.Domain.csproj` has no `ProjectReference`. You cannot accidentally write `_context.SaveChanges()` inside `Ticket`, because `_context` does not exist in that project. The architecture is enforced by the build, not by a code review.

### The API → Infrastructure compromise

Strictly, the outermost layer should not depend on Infrastructure. But `Program.cs` must call `services.AddInfrastructure(...)` from somewhere.

The alternatives are assembly scanning or a plugin loader — indirection that buys purity and costs comprehensibility. This template allows the reference and **contains** it instead: only `Program.cs` and `Extensions/MigrationExtensions.cs` may name an Infrastructure type. Controllers depend on Application interfaces only. The rule is written into `HelpDisk.API.csproj` so it is visible where it would be broken.

If you see `using HelpDisk.Infrastructure` in a controller, that is a bug.

---

## 2. The `Result` pattern

The most consequential decision in the template. Expected failures are **return values**, not exceptions.

```csharp
// Not this:
void Assign(string assigneeId);            // might throw; the signature won't say

// This:
Result Assign(string assigneeId);          // failure is part of the type
```

Three payoffs:

**Honest signatures.** "This can fail" is visible at the call site, not buried in a doc comment.

**No control flow by exception.** "Ticket not found" happens dozens of times a day during normal use. If it throws, your logs fill with routine noise and a real outage becomes invisible in it.

**Layer independence — the big one.** Domain returns `Error(ErrorType.NotFound)`. It does not return `404`, because Domain must not know HTTP exists. `ApiController` performs that translation at the edge, in one file. Put a gRPC front end on this and Domain and Application are untouched.

### Result vs exceptions: the dividing line

| | Mechanism | Becomes |
|---|---|---|
| Ticket not found; cannot assign a closed ticket; validation failed | `Result` | 4xx |
| Null reference; database down; a bug | exception | 500 |

The rule of thumb: **if a competent user could cause it by using the system normally, it is a `Result`. If it means something is broken, let it throw.** `GlobalExceptionHandler` catches the second kind.

### The cost, stated honestly

You must check `IsFailure` and propagate, every time. C# has no do-notation to do it for you, so a `Result`-heavy method has visible plumbing between the interesting lines. Miss a check and you carry on with a failed result — which is why `Result`'s constructor throws on impossible states and `Result<T>.Value` throws when read on a failure.

---

## 3. Domain-Driven Design, applied proportionally

### `Ticket` is a rich aggregate root

`Ticket` has private setters, a private constructor, a static `Create` factory returning `Result<Ticket>`, and behaviour methods that enforce invariants. There is no way to construct an invalid `Ticket`, and no way to change one except through a method that checks first.

Compare with the anemic style — public properties, rules in the service:

```csharp
ticket.Status = TicketStatus.InProgress;   // did anyone check it wasn't closed?
ticket.AssigneeId = request.AssigneeId;    // who else writes this field?
```

That works until a second caller — a bulk import, an admin screen, a background job — forgets one of the checks. The rule was never in one place, so it was never really a rule.

### `Category` is deliberately not

|  | `Ticket` | `Category` |
|---|---|---|
| Base class | `AggregateRoot<Guid>` | `Entity<Guid>` |
| Domain events | yes | none |
| Child entities | `Comments` | none |
| State machine | New / InProgress / Closed | none |
| Soft delete | yes | no |

A category is a lookup value with one rule. It gets a factory to enforce that rule and nothing else.

**This contrast is the most practically useful thing in the template.** The most common way DDD fails is applying full ceremony to everything, so a five-row lookup table acquires a factory, three events, and a specification. The team then concludes DDD is bureaucracy — and about what they built, they are right.

Match the machinery to the complexity of the rules. Most entities are Categories. A few are Tickets. The Tickets are the ones people argue about in meetings.

### Aggregate boundaries

`TicketComment` has an `internal` constructor and no repository. The only way to create one is `Ticket.AddComment`, which is what makes "you cannot comment on a closed ticket" enforceable. Add an `ITicketCommentRepository` and that rule becomes bypassable immediately.

`Ticket` references `Category` **by id**, with no navigation property — even though EF would provide one free. A navigation invites `ticket.Category.Name = "..."`: editing a second aggregate through the first, in one transaction, with none of `Category`'s own rules consulted. The cost is honest — showing a category name alongside a ticket now needs a second lookup.

### Where each kind of rule lives

| Rule | Lives in | Because |
|---|---|---|
| Title is required, max 200 chars | `Ticket.Create` **and** the validator | the aggregate guarantees it for every caller; the validator gives the API a friendly, all-at-once 400 |
| Cannot assign a closed ticket | `Ticket.Assign` | it depends only on the ticket's own state |
| The category must exist | `TicketService` | needs storage; an aggregate that reaches for a repository stops being testable without one |
| Category names are unique | `CategoryService` **and** a unique index | one instance cannot see the whole collection; and the service check is not atomic under concurrency |

That last row is worth dwelling on. `CategoryService` checks for a duplicate name to produce a friendly 409, but two requests can both pass the check before either writes. The unique index in `CategoryConfiguration` is what makes the rule *true*. **Application-level checks are for humans; database constraints are for correctness.** You usually want both.

---

## 4. Feature slicing without CQRS

Code is grouped by **feature** (`Features/Tickets/`), not by technical kind (`Services/`, `Dtos/`, `Validators/`). Delete the folder and the feature is gone, with nothing stranded in six other directories.

Inside the slice there is **one service class per feature** rather than a command/query pair per operation.

|  | One service (here) | CQRS + MediatR |
|---|---|---|
| Files per feature | ~6 | ~20 |
| Navigation | F12 from controller to logic | send a message, go find the handler |
| Cross-cutting concerns | called explicitly in each method | `IPipelineBehavior`, free across all handlers |
| Reads vs writes | one model | can diverge independently |
| Scales to | a feature one person holds in their head | large systems, many contributors |

Neither is wrong. CQRS earns its cost on large systems where reads and writes genuinely diverge. This template teaches *layers*, and one service per feature keeps the layers visible instead of burying them under a messaging pattern.

The concrete consequence of dropping MediatR: there is no `ValidationPipelineBehavior`, so `TicketService` calls its validator explicitly as the first statement of each method. More typing; nothing hidden.

---

## 5. Dependency inversion, twice

The pattern appears in two flavours worth distinguishing.

**Storage** — `ITicketRepository` is declared in **Domain**, next to the aggregate it serves, and implemented in Infrastructure. The consumer owns the contract; the provider obeys.

**Ambient values** — `ICurrentUser` is declared in **Application** and implemented in **the API project**, because it needs `HttpContext`.

That second one carries a lesson people usually miss: *"Infrastructure implements the interfaces"* is a rule of thumb, not a law. The real rule is that an inner-layer interface is implemented by whichever **outer** layer naturally owns the dependency. Storage concerns land in Infrastructure; request-scoped concerns land in the API. Both are outside Application, which is all the dependency rule requires.

### Why not `IGenericRepository<T>`?

Many codebases (including the MOJ reference this template was modelled on) use one generic repository for every entity. The trade:

- **For:** far less code; a new entity needs no new repository.
- **Against:** `GetQueryable()` hands an `IQueryable` to Application, so query construction moves into services. Two things follow. EF Core's translation rules leak upward — your service silently depends on what *this* provider can translate, so "swap the database" stops being real. And callers can compose queries the aggregate never sanctioned, such as loading a `TicketComment` without its `Ticket`.

This template uses one interface per aggregate with methods named for what the business wants. `ITicketRepository` is a finite, readable list of every way tickets can be read, and no caller can invent a new one.

---

## 6. Cross-cutting concerns, done at a choke point

Three EF Core interceptors implement behaviour that must be true of *everything*:

| Interceptor | When | Does |
|---|---|---|
| `SoftDeleteInterceptor` | before SQL | rewrites `Deleted` → `Modified` with `IsDeleted = true` |
| `AuditableEntityInterceptor` | before SQL | stamps `CreatedOnUtc` / `ModifiedOnUtc` |
| `DomainEventsInterceptor` | **after** SQL succeeds | dispatches domain events |

**Registration order is significant.** Soft delete must run before auditing, so the audit pass sees an entity already flipped to `Modified`. Reverse them and soft-deleted rows keep a stale `ModifiedOnUtc`.

The alternative to interceptors — remembering to set `ModifiedOnUtc` in every service method, and to append `.Where(x => !x.IsDeleted)` to every query — works until the one place that forgets. There is exactly one code path to the database; put the invariant on it.

### Domain events and the timing that matters

`Ticket.Assign` raises `TicketAssignedDomainEvent` and does nothing about it. `DomainEventsInterceptor` dispatches it from `SavedChangesAsync` — *after* the transaction commits — so you never announce an assignment that got rolled back.

The honest limitation: because the commit has already happened, a failing handler cannot undo it. If the notification fails, the ticket stays assigned and the notification is lost. For most reactions that is correct. When it is not, the answer is the **outbox pattern** — write the event to a table inside the same transaction and let a background worker deliver it with retries. Out of scope here; that is the term to search for.

`DomainEventDispatcher` is roughly thirty readable lines of reflection. If you have used MediatR's `Publish`, this is what it was doing for you.

---

## 7. Deliberate departures from the MOJ reference

HelpDisk was modelled on `MOJ/src/Training`. Where it differs, and why:

| # | MOJ | HelpDisk | Why |
|---|---|---|---|
| 1 | `BuildingBlock` + `Shared` solution folders | primitives inlined into the four projects | requirement: no building blocks |
| 2 | MediatR commands/queries/handlers | one service class per feature | requirement: no CQRS; see §4 |
| 3 | `IGenericRepository<T>` | `ITicketRepository` per aggregate | §5 |
| 4 | near-anemic entities, built by `_mapper.Map<Entity>(request)` | rich aggregate with factory and behaviour | §3 |
| 5 | `EnsureDatabaseInitializationAndUpToDate()` calls `services.BuildServiceProvider()` mid-registration and swallows migration exceptions | `app.ApplyMigrations()` on the built app, Development only, exceptions propagate | see below |
| 6 | `Result<T>.Value` returns `default!` on failure | throws | a silent null moves the crash somewhere unrelated |
| 7 | `IUnitOfWork.BeginTransactionAsync` returns `IDbContextTransaction` | returns `Task`; the EF type stays private | an EF type on a Domain interface drags EF Core into Domain |
| 8 | `net8.0` | `net10.0` | only SDK 10 is installed on the build machine |
| 9 | Serilog, Hangfire, MassTransit, localization, API versioning, permissions | none | scope |

**On #5**, plainly: building a second service provider during registration instantiates every singleton twice (ASP.NET Core ships analyzer `ASP0000` for exactly this), and swallowing a failed migration produces an app that starts, reports healthy, and fails on the first query against a table that was never created. Loud failure at startup beats quiet failure later.

**On #7**, the difference is one word in a signature and it decides whether the dependency rule is real. Same capability; no leak.

Departures #5, #6 and #7 are genuine defects in the reference, and a teaching template should not propagate them.

---

## 8. What is missing, and what adding it would cost

The useful exercise is not *what* is absent but *which layers would change*.

| Feature | Domain | Application | Infrastructure | API |
|---|---|---|---|---|
| JWT authentication | — | — | — | ✔ delete the fallback in `CurrentUser`, add middleware |
| Send real e-mail on assignment | — | ✔ declare `IEmailService`, edit one handler | ✔ implement it | — |
| Swap SQL Server → PostgreSQL | — | — | ✔ | — |
| Redis caching | — | ✔ declare `ICacheService` | ✔ implement it | — |
| Add a gRPC front end | — | — | — | ✔ new project |
| Structured logging (Serilog) | — | — | — | ✔ |
| New business rule on tickets | ✔ | — | — | — |

Read the **Domain** column. It is empty for every technical change and populated only for the business one.

That is the entire argument for this architecture, and it is worth stating to a room: you are not doing this for elegance. You are doing it so that the class describing when a ticket may be assigned never has to be reopened because someone changed the database.
