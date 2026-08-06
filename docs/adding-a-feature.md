# Adding a feature

A checklist for adding a feature to HelpDisk. Following it reproduces the **Category** slice exactly — so if the checklist and those files ever disagree, the checklist is what is wrong.

The running example is a feature called `Team`. Substitute your own name.

**Work outside-in? No — work inside-out.** Start at Domain and finish at the API. Each step compiles before you move on, and you are never guessing what the layer below will offer.

---

## Step 0 — Decide how much ceremony this feature needs

Before writing anything, answer one question: **does this thing have rules worth protecting?**

| Answer | Build it like | Which means |
|---|---|---|
| No — it is a lookup value with a name | `Category` | `Entity<Guid>`, a factory, maybe one behaviour method |
| Yes — it has a lifecycle, children, or invariants | `Ticket` | `AggregateRoot<Guid>`, behaviour methods, domain events |

Getting this wrong in the *cautious* direction is the expensive mistake. A lookup table dressed as an aggregate root — factory, events, specification, all unused — is how teams conclude DDD is bureaucracy.

The example below assumes a simple entity. Where an aggregate would differ is flagged.

---

## Step 1 — Domain

Create `src/HelpDisk.Domain/Teams/`.

### 1a. The errors — `TeamErrors.cs`

Write these **first**. It forces you to enumerate how the feature can fail before you write code that fails.

```csharp
public static class TeamErrors
{
    public static Error NotFound(Guid teamId) => Error.NotFound(
        "Team.NotFound", $"No team was found with id '{teamId}'.");

    public static readonly Error NameRequired = Error.Validation(
        "Team.NameRequired", "A team must have a name.");
}
```

Codes follow `Aggregate.Reason` so the origin is obvious in a log line.

Use `Error.Validation` when the *request* was malformed, and `Error.Conflict` when the request was fine but the entity's *state* forbids it. That distinction becomes 400 vs 409, and it tells the caller whether fixing their payload would help.

### 1b. The entity — `Team.cs`

```csharp
public sealed class Team : Entity<Guid>          // AggregateRoot<Guid> if it has
{                                                //   events or child entities
    public const int NameMaxLength = 100;

    private Team() { }                           // EF Core only

    private Team(Guid id, string name) : base(id) => Name = name;

    public string Name { get; private set; } = null!;

    public static Result<Team> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return TeamErrors.NameRequired;
        return new Team(Guid.NewGuid(), name.Trim());
    }
}
```

Non-negotiable:

- **private parameterless constructor** — EF needs it; nobody else may use it
- **private setters** — state changes go through methods that check first
- **static factory returning `Result<T>`** — a constructor can only refuse by throwing
- **`Guid.NewGuid()` in the factory** — the domain owns identity (this matters in step 3)

### 1c. The repository interface — `ITeamRepository.cs`

Declared **here in Domain**, implemented later in Infrastructure. That is the dependency inversion.

```csharp
public interface ITeamRepository
{
    Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Team team, CancellationToken cancellationToken = default);
}
```

Name methods for what the business wants, not how they are fetched. Never expose `IQueryable`. Never add a `SaveAsync` — `IUnitOfWork` owns that.

> **Aggregates only:** also add `Events/TeamCreatedDomainEvent.cs` as a `record ... : IDomainEvent`, named in the past tense, carrying ids rather than entities.

---

## Step 2 — Application

Create `src/HelpDisk.Application/Features/Teams/`.

### 2a. DTOs — `Dtos/TeamDtos.cs`

```csharp
public sealed record CreateTeamRequest(string Name);
public sealed record TeamResponse(Guid Id, string Name, DateTime CreatedOnUtc);
```

**Never accept or return an entity.** Requests bound directly to entities are mass assignment; entities returned as responses leak internal fields into your public contract the moment somebody adds one.

Leave out any field the caller should not control — anything derived from `ICurrentUser`, for instance.

### 2b. Validator — `Validators/CreateTeamRequestValidator.cs`

```csharp
public sealed class CreateTeamRequestValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Team.NameMaxLength);
    }
}
```

Registered automatically by assembly scan — no wiring needed.

Yes, this duplicates the check in `Team.Create`. That is deliberate: the validator reports *every* problem at once for a friendly 400; the factory guarantees the rule for *every* caller, including ones that never touch FluentValidation. The validator is a convenience; the aggregate is the guarantee.

Add `.IsInEnum()` on any enum property. Without it, `priority=99` binds happily.

### 2c. Service — `ITeamService.cs` + `TeamService.cs`

Every method returns `Result` or `Result<T>` and takes a `CancellationToken`.

Follow the five beats:

```csharp
public async Task<Result<Guid>> CreateAsync(CreateTeamRequest request, CancellationToken ct = default)
{
    // 1. validate shape
    var validation = await _createValidator.ValidateAsync(request, ct);
    if (!validation.IsValid)
        return Error.Validation("Validation.Failed",
            string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

    // 2. load / check anything referenced   (skip if nothing to check)

    // 3. let the domain do the work
    var teamResult = Team.Create(request.Name);
    if (teamResult.IsFailure) return teamResult.Error;

    // 4. persist
    await _teams.AddAsync(teamResult.Value, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    // 5. answer
    return teamResult.Value.Id;
}
```

**If you find yourself writing `if (team.Status == ...)` in this class, stop.** That rule belongs on the entity — a rule in a service only applies to callers who remember to use that service.

> **Mapping:** hand-write it for two or three properties, as `CategoryService` does. Add a Mapster `IRegister` config only when the shape is big enough to earn it — and map **entity → DTO only**. Never map a request onto an entity; that bypasses the factory and every rule in it.

### 2d. Register it — `Application/DependencyInjection.cs`

```csharp
services.AddScoped<ITeamService, TeamService>();
```

Scoped, always. Singleton would outlive the request's `DbContext`.

> **Aggregates only:** add `EventHandlers/`, implement `IDomainEventHandler<TeamCreatedDomainEvent>`, and register it explicitly here.

---

## Step 3 — Infrastructure

### 3a. Configuration — `Persistence/Configurations/TeamConfiguration.cs`

```csharp
public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();      // <-- see below
        builder.Property(t => t.Name).IsRequired().HasMaxLength(Team.NameMaxLength);
    }
}
```

Picked up automatically by `ApplyConfigurationsFromAssembly`. No attributes on the entity — mapping is a persistence concern and stays on this side of the wall.

> ### `ValueGeneratedNever()` — do not skip this line
>
> Your factory calls `Guid.NewGuid()`, so the **domain** generates the key. EF's default convention for a `Guid` key is `ValueGeneratedOnAdd` — "the store supplies this" — which feeds a heuristic: *key is store-generated and already has a value ⇒ this must be an existing row ⇒ mark it `Modified`*.
>
> For an entity you add via `AddAsync` that never fires, because the state is set explicitly. But for an entity EF discovers **through a navigation property** — i.e. any child of an aggregate — it does, and EF emits `UPDATE ... WHERE Id = @p` affecting zero rows, then throws `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0`.
>
> This bug was hit while building this template. See the long comment in `TicketCommentConfiguration.cs`.

Other things to set here when they apply:

- `.HasConversion<int>()` on enums, with explicit numeric values in the enum so stored data cannot shift meaning
- `.HasQueryFilter(x => !x.IsDeleted)` if the entity implements `ISoftDeleteEntity`
- `.HasIndex(...)` matching how your repository actually filters
- `.HasIndex(...).IsUnique()` for any uniqueness rule your service checks — the service check is not atomic
- `.OnDelete(DeleteBehavior.Restrict)` on references to other aggregates; the `Cascade` default deletes more than you meant
- `.Ignore(x => x.DomainEvents)` on aggregate roots — events are in-memory only
- `SetPropertyAccessMode(PropertyAccessMode.Field)` on any collection exposed read-only over a private backing field. Omit it and the collection silently never loads.

### 3b. Repository — `Persistence/Repositories/TeamRepository.cs`

```csharp
public sealed class TeamRepository : ITeamRepository
{
    private readonly AppDbContext _context;
    public TeamRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Set<Team>().AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await _context.Set<Team>().AnyAsync(t => t.Id == id, ct);

    public async Task AddAsync(Team team, CancellationToken ct = default) =>
        await _context.Set<Team>().AddAsync(team, ct);
}
```

`AsNoTracking()` on **read-only** queries only. Put it on a method whose result will be modified and the write silently never persists.

### 3c. `DbSet` and registration

In `AppDbContext`: `public DbSet<Team> Teams => Set<Team>();`

In `Infrastructure/DependencyInjection.cs`: `services.AddScoped<ITeamRepository, TeamRepository>();`

### 3d. Migration

```bash
dotnet ef migrations add AddTeams \
  --project src/HelpDisk.Infrastructure \
  --startup-project src/HelpDisk.API \
  --output-dir Persistence/Migrations
```

**Read the generated file before committing it.** Check the foreign key delete behaviour, the column types, and that nothing you did not intend is being dropped.

---

## Step 4 — API

`src/HelpDisk.API/Controllers/TeamsController.cs`:

```csharp
[Route("api/teams")]
public sealed class TeamsController : ApiController
{
    private readonly ITeamService _teamService;
    public TeamsController(ITeamService teamService) => _teamService = teamService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request, CancellationToken ct)
    {
        var result = await _teamService.CreateAsync(request, ct);
        return HandleResult(result);
    }
}
```

Three lines per action. **No logic, no validation, no mapping, no `try`/`catch`.**

Inherit `ApiController` — it maps `Result` to a status code. Do not write `NotFound()` or `BadRequest()` yourself; that mapping lives in exactly one file and should stay there.

Name routes after what the business *does*, not after table columns: `PUT /api/tickets/{id}/assign`, not a general-purpose PATCH with an `assigneeId` field. Distinct actions have distinct rules and distinct events.

No registration needed — controllers are discovered automatically.

---

## Step 5 — Verify

```bash
dotnet build                                    # expect 0 warnings, 0 errors
dotnet run --project src/HelpDisk.API           # migrations apply on startup
```

Then exercise it in Swagger. **Test at least one failure path**, not just the happy one — the failure paths are where the architecture either holds or does not.

---

## The review checklist

Before you call it done:

- [ ] `HelpDisk.Domain.csproj` still has no `ProjectReference`
- [ ] No entity crosses a layer boundary — DTOs in, DTOs out
- [ ] No `IQueryable` escapes the repository
- [ ] No business rule sits in the service that could sit on the entity
- [ ] Every service method returns `Result` / `Result<T>` and takes a `CancellationToken`
- [ ] No HTTP status code appears outside `ApiController`
- [ ] `ValueGeneratedNever()` is set on any key the domain generates
- [ ] `.IsUnique()` backs any uniqueness rule the service checks
- [ ] The migration was read, not just generated
- [ ] Comments explain **why**, not what — this is a teaching repository, and that is the deliverable
