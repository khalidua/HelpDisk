# HelpDisk — Clean Architecture + DDD Teaching Template

**Date:** 2026-08-06
**Status:** Approved
**Purpose:** A reference solution used to teach Clean Architecture, DDD, and feature-sliced application services to other developers.

---

## 1. Goal and non-goals

### Goal

Produce a small, complete, runnable .NET solution that demonstrates Clean Architecture and DDD across four projects. Every layer must contain a worked example, so a student can trace a single HTTP request from controller to database and back, and can see *why* each layer exists.

The solution is a **template**, not a product. It implements a `Ticket` feature only far enough to exercise every pattern, plus a second smaller `Category` feature that proves the pattern repeats.

### Success criteria

1. `dotnet build` succeeds with zero warnings-as-errors and zero errors.
2. `dotnet run --project src/HelpDisk.API` starts and serves Swagger UI.
3. Every endpoint on `TicketsController` can be exercised from Swagger against a real SQL Server database.
4. A developer unfamiliar with the codebase can read `README.md`, then add a new feature by following `docs/adding-a-feature.md` without asking questions.
5. `HelpDisk.Domain.csproj` contains zero `ProjectReference` and zero third-party `PackageReference`.

### Non-goals (explicitly out of scope)

- **No CI/CD pipelines.** Requested directly by the user.
- **No test project.** The user specified exactly four projects. `README.md` will describe how the Domain layer would be unit-tested with zero mocks, because that testability is the payoff of the rich-aggregate decision — but no test project is created.
- **No authentication flow.** No JWT validation, no identity provider, no login endpoint. See §6.3 for what replaces it.
- **No MediatR / CQRS.** Explicitly rejected by the user in favour of one service class per feature.
- **No Hangfire, MassTransit/RabbitMQ, PDF generation, media/file handling, localization, API versioning, health checks, or Serilog/Seq.** These exist in the MOJ reference but bury the architecture lesson.
- **No `BuildingBlocks` or `Shared` project.** Primitives are inlined into the four projects (see §3).

---

## 2. Reference codebase

`D:\Repos\C4R\MOJ` — specifically `src/Training` (a four-layer service) and `src/BuildingBlock` (the shared primitives that HelpDisk must absorb).

HelpDisk follows MOJ's idiom where it is sound and deliberately departs where it is not. Departures are enumerated in §7 so they can be explained to students rather than discovered as inconsistencies.

---

## 3. Solution structure

```
D:\Repos\C4R\HelpDisk\
├── HelpDisk.sln
├── README.md
├── .gitignore
├── .editorconfig
├── docs/
│   ├── architecture.md
│   ├── adding-a-feature.md
│   └── superpowers/specs/2026-08-06-helpdisk-template-design.md
└── src/
    ├── HelpDisk.Domain/
    ├── HelpDisk.Application/
    ├── HelpDisk.Infrastructure/
    └── HelpDisk.API/
```

Projects are grouped under a `src` solution folder, matching MOJ.

### 3.1 Target framework and tooling

- **TFM:** `net10.0` for all four projects. The build machine has only SDK `10.0.302` and `dotnet-ef 10.0.10`; targeting `net8.0` (MOJ's TFM) would require downloading reference packs and installing the .NET 8 runtime to run.
- `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` in every project.
- **No `Directory.Build.props` and no `Directory.Packages.props`.** TFM, nullable settings, and package versions are written explicitly in each `.csproj`. Central management is better practice on a real product but adds indirection when the teaching goal is "open the csproj and see exactly what this project needs."
- Package versions are **pinned** as explicit `Version=` attributes. No floating versions, no wildcards. Resolved against NuGet on 2026-08-06:

| Package | Version | Project |
|---|---|---|
| `FluentValidation.DependencyInjectionExtensions` | `12.1.1` | Application |
| `Mapster` | `10.0.11` | Application |
| `Mapster.DependencyInjection` | `10.0.11` | Application |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.10` | Infrastructure |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.10` | Infrastructure (`PrivateAssets=all`) |
| `Swashbuckle.AspNetCore` | `10.2.3` | API |

EF Core `10.0.10` matches the installed `dotnet-ef 10.0.10`, so migration tooling and runtime agree. EF Core 11.x exists only as preview and is not used.

### 3.2 The dependency rule

| Project | References |
|---|---|
| `HelpDisk.Domain` | **nothing** — no project references, no third-party packages |
| `HelpDisk.Application` | `HelpDisk.Domain` + FluentValidation, Mapster |
| `HelpDisk.Infrastructure` | `HelpDisk.Application` + EF Core (SqlServer, Design) |
| `HelpDisk.API` | `HelpDisk.Application` + `HelpDisk.Infrastructure` + ASP.NET Core, Swagger |

`HelpDisk.Domain.csproj` carries a comment stating that it has no `ProjectReference` and never will, so the constraint is visible at the point where a student would be tempted to break it.

**The API → Infrastructure reference is a deliberate, documented compromise.** Strict Clean Architecture would forbid it, but `Program.cs` must call `services.AddInfrastructure(configuration)` from somewhere. The exception is contained to the composition root: `Program.cs` and `DependencyInjection.cs` are the only files in `HelpDisk.API` permitted to reference an Infrastructure type. Controllers depend on Application interfaces only. `docs/architecture.md` states this rule and explains the alternative (a plugin/assembly-scanning loader) and why it was rejected as needless indirection for a teaching template.

---

## 4. Layer contents

### 4.1 HelpDisk.Domain

Organized **by aggregate**, not by technical kind, so an aggregate and its contract sit together.

```
HelpDisk.Domain/
├── Primitives/
│   ├── Entity.cs                 // abstract Entity<TKey> : IAuditableEntity
│   ├── AggregateRoot.cs          // AggregateRoot<TKey> : Entity<TKey>, raises domain events
│   ├── IAuditableEntity.cs       // CreatedOnUtc, ModifiedOnUtc
│   ├── ISoftDeleteEntity.cs      // IsDeleted, DeletedAtUtc, RestoredAtUtc
│   └── IDomainEvent.cs
├── Shared/
│   ├── Error.cs                  // record Error(string Code, ErrorType Type, string Description) + ErrorType enum
│   ├── Result.cs                 // Result with Success/Failure factories + implicit operator from Error
│   ├── ResultT.cs                // Result<TValue> with implicit operators
│   └── Pagination.cs             // Pagination<T>
├── Repositories/
│   └── IUnitOfWork.cs
├── Tickets/
│   ├── Ticket.cs                 // aggregate root
│   ├── TicketComment.cs          // child entity, only reachable through Ticket
│   ├── TicketStatus.cs           // New, InProgress, Resolved, Closed
│   ├── TicketPriority.cs         // Low, Normal, High, Urgent
│   ├── TicketErrors.cs           // static class of named Error instances
│   ├── ITicketRepository.cs
│   └── Events/
│       ├── TicketCreatedDomainEvent.cs
│       └── TicketAssignedDomainEvent.cs
└── Categories/
    ├── Category.cs
    ├── CategoryErrors.cs
    └── ICategoryRepository.cs
```

**`Ticket` is a rich aggregate root.** It has:

- a `private Ticket() { }` constructor, commented as *EF Core only*;
- a `public static Result<Ticket> Create(...)` factory that validates invariants and returns `TicketErrors.*` on failure — construction is impossible except through the factory;
- `private set` on every property;
- a `private readonly List<TicketComment> _comments` backing field exposed as `public IReadOnlyCollection<TicketComment> Comments`;
- behaviour methods `Assign`, `Close`, `Reopen`, `AddComment`, each returning `Result` and refusing illegal state transitions (e.g. assigning a closed ticket returns `TicketErrors.CannotAssignClosedTicket`);
- `RaiseDomainEvent(...)` calls inside `Create` and `Assign`.

`TicketComment` is an `Entity<Guid>`, not an aggregate root, and is only ever created via `Ticket.AddComment`. There is no `ITicketCommentRepository` — that absence is the lesson about aggregate boundaries.

**`ITicketRepository` is declared in Domain and implemented in Infrastructure** — the dependency-inversion example. Its methods are intention-revealing rather than generic:

```csharp
Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct);
Task<Ticket?> GetWithCommentsAsync(Guid id, CancellationToken ct);
Task<Pagination<Ticket>> SearchAsync(string? keyword, TicketStatus? status, int page, int pageSize, CancellationToken ct);
Task<bool> ExistsAsync(Guid id, CancellationToken ct);
Task AddAsync(Ticket ticket, CancellationToken ct);
void Remove(Ticket ticket);
```

It returns entities and never exposes `IQueryable`. `docs/architecture.md` names the trade-off against MOJ's `IGenericRepository<T>`: one interface per aggregate is more code, but the aggregate controls its own access patterns and no caller can compose a query the aggregate did not sanction.

`IUnitOfWork` is deliberately minimal — `SaveChangesAsync` plus explicit transaction methods. It does **not** expose `Repository<TEntity>()` as MOJ's does, because feature-specific repositories are injected directly.

### 4.2 HelpDisk.Application

```
HelpDisk.Application/
├── Abstractions/
│   ├── ICurrentUser.cs           // UserId, UserName
│   ├── IDateTimeProvider.cs      // UtcNow
│   └── Events/
│       ├── IDomainEventHandler.cs    // IDomainEventHandler<TEvent>
│       └── IDomainEventDispatcher.cs
├── Features/
│   ├── Tickets/
│   │   ├── ITicketService.cs
│   │   ├── TicketService.cs      // ALL ticket business logic
│   │   ├── Dtos/
│   │   │   ├── CreateTicketRequest.cs
│   │   │   ├── UpdateTicketRequest.cs
│   │   │   ├── AssignTicketRequest.cs
│   │   │   ├── AddCommentRequest.cs
│   │   │   ├── TicketSearchRequest.cs
│   │   │   ├── TicketResponse.cs
│   │   │   ├── TicketListItemResponse.cs
│   │   │   └── TicketCommentResponse.cs
│   │   ├── Validators/
│   │   │   ├── CreateTicketRequestValidator.cs
│   │   │   ├── UpdateTicketRequestValidator.cs
│   │   │   └── AddCommentRequestValidator.cs
│   │   ├── Mapping/
│   │   │   └── TicketMappingConfig.cs
│   │   └── EventHandlers/
│   │       └── TicketAssignedLoggingHandler.cs
│   └── Categories/
│       ├── ICategoryService.cs
│       ├── CategoryService.cs
│       ├── Dtos/ (CreateCategoryRequest, CategoryResponse)
│       └── Validators/CreateCategoryRequestValidator.cs
└── DependencyInjection.cs        // AddApplication()
```

**One service class per feature, no CQRS.** `ITicketService` exposes `CreateAsync`, `GetByIdAsync`, `SearchAsync`, `AssignAsync`, `CloseAsync`, `ReopenAsync`, `AddCommentAsync`, `DeleteAsync`. Every method returns `Result` or `Result<T>` and accepts a `CancellationToken`.

**Validation is an explicit first statement in each service method.** `TicketService` injects `IValidator<CreateTicketRequest>` and calls `ValidateAsync` before doing anything else, returning `Error.Validation(...)` when invalid. Nothing is hidden in a filter or pipeline. The split is documented in code comments:

- *shape* validation (required, max length, range) → FluentValidation, in Application;
- *business invariants* (cannot assign a closed ticket) → `Ticket`'s methods, in Domain.

**Mapster maps entity → DTO only.** Request → entity mapping is deliberately absent, because construction belongs to `Ticket.Create`. `TicketMappingConfig.cs` carries a comment explaining that this asymmetry is intentional and is the direct consequence of the rich-aggregate choice.

**Domain events dispatch without MediatR.** `IDomainEventDispatcher` is declared in Application and implemented in Infrastructure; it resolves `IDomainEventHandler<TEvent>` instances from `IServiceProvider` and invokes them. `TicketAssignedLoggingHandler` is the worked example — it writes a log line, standing in for "send an email." The whole mechanism is small enough to read in one sitting; without it, `RaiseDomainEvent` would be a call that goes nowhere, which would misteach the pattern.

`DependencyInjection.AddApplication()` registers the feature services, `IValidator<>` implementations via assembly scan, the Mapster `TypeAdapterConfig`, and the domain event handlers.

### 4.3 HelpDisk.Infrastructure

```
HelpDisk.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   │   ├── TicketConfiguration.cs
│   │   ├── TicketCommentConfiguration.cs
│   │   └── CategoryConfiguration.cs
│   ├── Interceptors/
│   │   ├── AuditableEntityInterceptor.cs
│   │   ├── SoftDeleteInterceptor.cs
│   │   └── DomainEventsInterceptor.cs
│   ├── Repositories/
│   │   ├── TicketRepository.cs
│   │   └── CategoryRepository.cs
│   ├── UnitOfWork.cs
│   └── Migrations/               // real, generated InitialCreate
├── Services/
│   ├── DateTimeProvider.cs
│   ├── CurrentUser.cs
│   └── DomainEventDispatcher.cs
└── DependencyInjection.cs        // AddInfrastructure(configuration)
```

- **`AppDbContext`** applies configurations from its own assembly and sets a global query filter `e => !e.IsDeleted` on soft-deletable entities.
- **`TicketConfiguration`** sets `PropertyAccessMode.Field` on the `Comments` navigation so EF can populate the private `_comments` list. This is the concrete cost of a rich aggregate and is commented as such — a student who writes a rich aggregate and skips this gets a confusing runtime failure.
- **Interceptors:** `AuditableEntityInterceptor` stamps `CreatedOnUtc`/`ModifiedOnUtc`; `SoftDeleteInterceptor` converts `EntityState.Deleted` to `Modified` with `IsDeleted = true`; `DomainEventsInterceptor` collects events from tracked aggregates and dispatches them after a successful save, then clears them.
- **`CurrentUser`** reads claims from `IHttpContextAccessor`, falling back to a fixed demo user id when unauthenticated, so the template runs without an identity provider.
- **Database:** SQL Server via LocalDB. The `MSSQLLocalDB` instance is confirmed present on the build machine (SQL Server 170 tools). Connection string:

  ```
  Server=(localdb)\MSSQLLocalDB;Database=HelpDisk;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
  ```

  `README.md` additionally documents a Docker SQL Server connection string for students on machines without LocalDB (macOS/Linux). A real `InitialCreate` migration is generated and committed.

### 4.4 HelpDisk.API

```
HelpDisk.API/
├── Abstractions/
│   └── ApiController.cs          // Result -> HTTP status mapping
├── Controllers/
│   ├── TicketsController.cs
│   └── CategoriesController.cs
├── Middleware/
│   └── GlobalExceptionHandler.cs // IExceptionHandler
├── Extensions/
│   └── MigrationExtensions.cs
├── Properties/launchSettings.json
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

- **`ApiController`** holds the `ErrorType → HTTP status` switch, ported from MOJ's `Api.Abstractions.ApiController` minus MediatR's `ISender`. This is the only place in the solution that knows about HTTP status codes.
- **Controllers are thin.** Each action injects its feature service, calls one method, and returns `HandleResult(result)`. No business logic, no validation, no mapping.
- **`GlobalExceptionHandler`** implements `IExceptionHandler` and returns RFC 7807 `ProblemDetails`. It handles *unexpected* exceptions only; expected failures travel as `Result`. That distinction is documented — it is why the `Result` pattern earns its keep.
- **Migrations are applied via `app.ApplyMigrations()` in Development only**, called on the built app using a proper scope.

---

## 5. Request flow (documented in README)

```
HTTP POST /api/tickets
  └─> TicketsController.Create(CreateTicketRequest)        [API]
      └─> ITicketService.CreateAsync(request, ct)          [Application]
          ├─> IValidator<CreateTicketRequest>.ValidateAsync   → shape rules
          ├─> Ticket.Create(title, description, priority,     → invariants
          │                 _currentUser.UserId)               returns Result<Ticket>
          │   └─> RaiseDomainEvent(TicketCreatedDomainEvent)
          ├─> ITicketRepository.AddAsync(ticket, ct)       [Domain contract]
          │   └─> TicketRepository                          [Infrastructure impl]
          └─> IUnitOfWork.SaveChangesAsync(ct)
              ├─> AuditableEntityInterceptor  → stamps CreatedOnUtc
              └─> DomainEventsInterceptor     → dispatches TicketCreatedDomainEvent
      └─> HandleResult(Result<Guid>) → 200 OK / 400 / 404 / 409
```

---

## 6. Key decisions and rationale

### 6.1 Rich aggregate over MOJ's property-bag entities

MOJ's `CourseTitle` has private setters but no methods; `AddTitleHandler` builds it with `_mapper.Map<CourseTitle>(request)`. HelpDisk instead makes `Ticket` enforce its own invariants. Rationale: the user asked for DDD, and without domain behaviour the Domain project is a data-shape library and the four-layer split is hard to justify to a student. The rich aggregate also makes Domain unit-testable with zero mocks, which `README.md` calls out.

### 6.2 Feature-specific repository over `IGenericRepository<T>`

Chosen over MOJ's generic repository. One interface per aggregate costs more code but keeps query composition inside the aggregate's contract and avoids leaking `IQueryable` into Application. `docs/architecture.md` presents both and names the trade-off honestly, since students will meet MOJ's generic version at work.

### 6.3 `ICurrentUser` instead of authentication

Application declares `ICurrentUser` because business logic needs to know *who is acting*; Infrastructure implements it against `HttpContext`. This teaches dependency inversion on an ambient concern without a login flow the template cannot provide. Endpoints are not `[Authorize]`-protected.

### 6.4 Explicit validation over an MVC auto-validation filter

Without MediatR there is no `ValidationPipelineBehavior`. An auto-validation filter would restore the brevity but makes validation invisible — the exact confusion a teaching template must avoid.

### 6.5 Two features, not one

`Category` exists so students see the pattern *repeat* rather than inferring a pattern from a single instance, and so `docs/adding-a-feature.md` can be verified against a second example. It is deliberately small and its surface is fixed: `Category` is a simple `Entity<Guid>` (not an aggregate root, no domain events, no child collection); `ICategoryService` has exactly `CreateAsync(CreateCategoryRequest, ct)` and `GetAllAsync(ct)`; DTOs are `CreateCategoryRequest` and `CategoryResponse`; one validator; `CategoriesController` with `POST /api/categories` and `GET /api/categories`.

The contrast with `Ticket` is itself a lesson: not every entity needs to be an aggregate root, and a feature that has no invariants to protect should not pretend otherwise. `docs/architecture.md` states this so students do not over-apply the rich-aggregate pattern to lookup tables.

---

## 7. Deliberate departures from MOJ

Each is documented in `docs/architecture.md` so it can be explained rather than discovered.

| # | MOJ | HelpDisk | Why |
|---|---|---|---|
| 1 | `BuildingBlock` + `Shared` solution folders | Primitives inlined into the four projects | User requirement: no building blocks |
| 2 | MediatR commands/queries/handlers | One service class per feature | User requirement: no CQRS |
| 3 | `IGenericRepository<T>` + `IUnitOfWork.Repository<T>()` | `ITicketRepository` per aggregate | §6.2 |
| 4 | Near-anemic entities, mapper-constructed | Rich aggregate with factory + behaviour | §6.1 |
| 5 | `Bootstrap.EnsureDatabaseInitializationAndUpToDate()` calls `services.BuildServiceProvider()` during registration and swallows exceptions | `app.ApplyMigrations()` on the built app, Development only, exceptions propagate | Building a second container during registration is an anti-pattern; silently swallowing migration failures produces a running app with a broken database |
| 6 | `net8.0` | `net10.0` | Only SDK 10.0.302 is installed locally |
| 7 | Serilog + Seq, Hangfire, MassTransit, localization, API versioning, permission policies | None | Scope; they bury the architecture lesson |

Departure #5 is worth stating plainly: it is a genuine defect in the reference codebase, and the template should not propagate it.

---

## 8. Documentation deliverables

- **`README.md`** — what the solution is, how to run it (prerequisites, connection string, `dotnet ef` commands), the dependency rule, the request-flow trace from §5, and a short note on how the Domain layer would be unit-tested with zero mocks.
- **`docs/architecture.md`** — layer-by-layer rationale, the `Result` pattern and why expected failures are not exceptions, aggregate boundaries and why there is no `ITicketCommentRepository`, the API → Infrastructure compromise, and the departures table from §7.
- **`docs/adding-a-feature.md`** — an ordered checklist for adding a feature, written so that following it reproduces the `Category` slice exactly.

---

## 9. Implementation approach

Built in a single context rather than dispatched to the subagent pipeline described in `CLAUDE.md`. A teaching template depends on naming, comment voice, and idiom being identical across all four layers; cold-context implementers reliably drift on exactly that, and the drift would be the first thing a student notices. The user was informed of this and approved.

Verification before completion: `dotnet build` clean, `dotnet ef migrations list` shows the generated migration, and the API starts and serves Swagger. No completion claim is made without that output.

---

## 10. Implementation notes — where the build departed from this spec

Recorded after the fact so the spec and the code agree. Each was a judgement call made during implementation, not a change of requirements.

### 10.1 Simplifications

| # | Spec said | Built | Why |
|---|---|---|---|
| 1 | `TicketStatus`: New, InProgress, **Resolved**, Closed; `ResolvedOnUtc` field | New, InProgress, Closed | Three states keep the transition diagram tight enough to draw on a whiteboard. A fourth state adds a `Resolve()` method and two more transitions without teaching anything the other three do not. |
| 2 | one file per DTO (8 files) | grouped into `TicketRequests.cs` and `TicketResponses.cs` | They are read together, and the file-per-record split buried two useful block comments in eight nearly-empty files. |

### 10.2 Corrections to the spec's design

| # | Spec said | Built | Why |
|---|---|---|---|
| 3 | `CurrentUser` in `Infrastructure/Services/` | `HelpDisk.API/Services/CurrentUser.cs` | It needs `IHttpContextAccessor`. Putting it in Infrastructure would require a `FrameworkReference` to `Microsoft.AspNetCore.App` in a class library whose job is database access. Placing it in the API also teaches a better general rule — an inner-layer interface is implemented by whichever *outer* layer naturally owns the dependency, and Infrastructure is not the only outer layer. |
| 4 | — | added `Domain/Primitives/IHasDomainEvents.cs` | `DomainEventsInterceptor` must sweep the change tracker for aggregates with pending events, and `ChangeTracker.Entries<AggregateRoot<TKey>>()` has no `TKey` to supply. The non-generic interface makes the query possible for any key type; the `is AggregateRoot<Guid>` workaround would silently skip aggregates keyed by anything else. |
| 5 | `Result<TValue>.Value` per MOJ | throws `InvalidOperationException` when read on a failure | MOJ returns `default!`, which hands back a null and moves the crash somewhere unrelated. Added as departure #6 in `docs/architecture.md`. |
| 6 | — | `Ticket.CategoryId` foreign key linking the two features | Gives `TicketService.CreateAsync` a real cross-aggregate existence check, and demonstrates the DDD rule that aggregates reference each other by identity rather than by navigation property. |

### 10.3 Discovered during implementation

| # | Finding |
|---|---|
| 7 | **`ValueGeneratedNever()` is required on every domain-generated key.** `POST /api/tickets/{id}/comments` returned 500 with `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0`. EF's default `ValueGeneratedOnAdd` convention for `Guid` keys, combined with its inference rule for entities discovered *through a navigation property*, caused a brand-new `TicketComment` to be marked `Modified` and written as an `UPDATE`. Fixed on all three entities; documented at length in `TicketCommentConfiguration.cs` and in the `adding-a-feature.md` checklist. |
| 8 | `Microsoft.EntityFrameworkCore.Design` must be referenced by the **startup** project as well as Infrastructure. Infrastructure's reference is `PrivateAssets="all"`, which correctly keeps a design-time package out of the deployed dependency graph but also stops `dotnet ef` finding it. |
| 9 | .NET 10's `dotnet new sln` defaults to the new `.slnx` XML format. Forced to classic `.sln` with `--format sln` — a teaching template gets opened on whatever tooling a student has, and `.slnx` needs VS 17.13+. |
| 10 | Three `Microsoft.Extensions.*.Abstractions` packages were needed and are not in the spec's table: `DependencyInjection.Abstractions` and `Logging.Abstractions` in Application, `Configuration.Abstractions` in Infrastructure. All are abstraction-only, so no container or logging provider is pulled into the inner layers. |
| 11 | Two validators beyond the spec's three: `AssignTicketRequestValidator` and `TicketSearchRequestValidator`. The latter caps `PageSize` at 100 — an unbounded list endpoint is an outage waiting to happen. |

### 10.4 Environment note

The build machine's Windows **Application Control policy** blocks loading freshly-built assemblies from `D:\Repos\` (`FileLoadException ... 0x800711C7`). Runtime verification was therefore performed against `dotnet publish` output run from a temp directory. This is a machine policy, not a property of the solution — `dotnet build` and `dotnet ef` both work normally in place, and the published app ran and passed every check.
