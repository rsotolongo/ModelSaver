# CLAUDE.md — <SolutionName> (.NET 10 / C# 14 / ASP.NET Core Web API / EF Core 10)

## 0. How to apply this file
- This file is self-contained for the whole team.
- The root `.editorconfig` sets every analyzer diagnostic to **error** and is the final word on style. If anything here disagrees with it, the `.editorconfig` wins. Write code that builds clean the first time rather than fixing it after the build fails.

### Baseline (applies everywhere)
- `var` **only when the type is apparent** on the right-hand side (`new T(...)`, a cast, `as T`). Everywhere else write the type, including built-in types: `int count = lines.Count;`, `Result<Guid, Error> result = await ...;` (IDE0007/IDE0008).
- Block bodies for methods, constructors and local functions. Expression bodies (`=>`) for properties, indexers, accessors, operators and lambdas (IDE0021/IDE0022).
- Braces on every `if`/`else`/`for`/`foreach`/`while`, even single-line ones (IDE0011, SA1503). Opening braces go on their own line, and each statement goes on its own line.
- One type per file, with the file named after the type (MA0048, SA1402). This includes the command, its handler and its validator.
- Generic constraints (`where T : ...`) and constructor initializers (`: base(...)`) go on their own line (SA1127, SA1128).
- Trailing comma after the last item of every multi-line initializer, enum and switch expression (SA1413, MA0007).
- `string.Empty`, never `""` (RCS1078). Name `null`/`true`/`false` literal arguments: `new Result(value, error: null, isSuccess: true)` (MA0003).
- Lambdas that capture nothing are `static`: `static sp => sp.GetRequiredService<AppDbContext>()` (IDE0320).
- File-scoped namespaces; `using` directives outside the namespace; top-level statements in `Program.cs`; records for DTOs and immutable shapes; primary constructors wherever possible (IDE0290); no `#region`.
- Private fields are `_camelCase`. Static readonly fields are `PascalCase`.
- XML docs (`///`) are **required on every public type and member** (CS1591 is an error, and the documentation file is generated). A one-line `<summary>` is enough. Prefer `internal` for anything not consumed across projects; tests reach internals through `InternalsVisibleTo`.
- Other comments: none by default. Only a non-obvious why, an invariant or ordering callers must honor, or a deliberate deviation. No commented-out code (S125), no unowned `// TODO`.
- **No in-source suppressions**: no `#pragma warning disable`, no `[SuppressMessage]`, no `GlobalSuppressions.cs` (S1309). Fix the code. If a rule is wrong for a case, propose a scoped `.editorconfig` change to the team lead instead.
- Async: every method returning `Task`/`ValueTask` ends in `Async` (MA0137, RCS1046). `CancellationToken` comes last and is threaded all the way down. Never `.Result` or `.Wait()`.
- **Every `await` uses `.ConfigureAwait(false)`** in production code, including `await using` and `await foreach` (CA2007). In xUnit tests use `.ConfigureAwait(true)` instead, because xUnit1030 forbids `false` in tests.
- Expected failures → `Result<TValue, TError>`; exceptions only cross boundaries. Every HTTP error is RFC 9457 ProblemDetails via `GlobalExceptionHandler` (§12).
- Time comes from an injected `TimeProvider` (§17).
- Logging: **every** log call goes through a `[LoggerMessage]` source-generated method (CA1848). A `catch` either logs or rethrows, never both (S2139). Secrets, tokens and PII never reach a sink (§15).
- Tests: xUnit; PascalCase names **without underscores** (CA1707) following `<Method><Scenario><Expected>`, e.g. `AddLineWhenOrderIsNotPendingReturnsNotPending`; `[Theory]` + `[InlineData]` over duplicated `[Fact]`s; one behavior per test. NSubstitute + FluentAssertions pinned `[7,8)` (v8+ is commercial) or Shouldly. Integration tests use Testcontainers.
- Greenfield packages (existing choices win): Serilog + `Serilog.AspNetCore`, FluentValidation, `Microsoft.Extensions.Http.Resilience` (Polly v8), `IHttpClientFactory`.

### Precedence
- **The `.editorconfig` wins**, then **existing conventions in the solution**, then the defaults here. Before writing code, open a sibling feature and mirror its folders, names, registration and error style.
- When existing code deviates from a rule here, **flag it** in your summary (file:line, rule, suggested fix). Don't silently "fix", rewrite or migrate it in the same change unless asked.
- Never add a second way of doing something the solution already does one way: no second Result type, mediator, validation engine, error shape, logging provider or time abstraction.
- **(verify)** marks a rule from an unconfirmed source. Check it against the SDK, docs or a test before relying on it.
- Comments inside the code shapes below annotate the rule for you. Don't copy them into production code.
- The code shapes put several types in one block and leave out `///` docs to save space. In the solution, each type goes in its own file and every public member gets its XML doc.

## 1. Project overview & commands
| Item | Value |
|---|---|
| Solution | `<SolutionName>.slnx` |
| Projects | `src/<SolutionName>.Domain`, `.Application`, `.Infrastructure`, `.Api` (+ `<AppHostProject>` if Aspire) |
| Tests | `tests/<SolutionName>.Domain.Tests`, `.Application.Tests`, `.Api.IntegrationTests`, `.ArchitectureTests` |
| Database / provider | `<SqlServer \| PostgreSQL>` |
| Identity provider | `<IdP name / Authority URL config key>` |
| Message broker | `<none \| RabbitMQ \| Azure Service Bus \| ...>` |
| Mediator | `<own dispatcher \| Mediator (source-gen) \| MediatR (licensed)>` |

```bash
dotnet build <SolutionName>.slnx -c Release          # warnings are errors
dotnet test <SolutionName>.slnx                      # integration tests need Docker (Testcontainers)
dotnet format <SolutionName>.slnx --verify-no-changes
dotnet ef migrations add <Name> --project src/<SolutionName>.Infrastructure \
  --startup-project src/<SolutionName>.Api --context AppDbContext
dotnet run --project src/<SolutionName>.Api           # or the Aspire AppHost
```
- A task is done only when build, tests and the format check all pass.

## 2. Solution layout & dependency rules
```text
Domain          -> nothing (zero ProjectReference, zero PackageReference)
Application     -> Domain
Infrastructure  -> Application, Domain        (EF Core providers, HTTP clients, broker live here only)
Api             -> Application, Infrastructure (Infrastructure only for DI composition in Program.cs)
```
| Layer | Owns | Never contains |
|---|---|---|
| Domain | Aggregates, entities, value objects, strongly typed IDs, domain events, `Error`/`Result`, `XxxErrors`, repository interfaces, domain services | EF Core, ASP.NET Core, MediatR, FluentValidation, `Microsoft.Extensions.*`, logging, data annotations, localized text |
| Application | Commands/queries/handlers, validators, pipeline behaviors, `IUnitOfWork`, ports (`ICurrentUser`, `ITenantContext`, `IEmailSender`, ...), DTOs, domain-event handlers that orchestrate | Infrastructure types, `HttpContext`, Serilog types, provider packages |
| Infrastructure | `AppDbContext`, `IEntityTypeConfiguration<T>`, migrations, interceptors, repositories (`internal sealed`), outbox/inbox processors, typed HTTP clients, auth handlers | Use-case handlers, business rules |
| Api | `IEndpoint` classes, request/response contracts, `Result` → HTTP mapping, middleware, `Program.cs`, `ICurrentUser` implementation over `HttpContext` | Business rules, `DbContext` usage, try/catch in endpoints |

- Each project exposes one `AddDomain/AddApplication/AddInfrastructure/AddApi(...)` `IServiceCollection` extension. `Program.cs` chains them.
- Register explicitly. Scan assemblies only for homogeneous families (handlers, validators, endpoints, `IEntityTypeConfiguration`), always from an explicit marker type (`typeof(IApplicationMarker).Assembly`), never `AppDomain.CurrentDomain.GetAssemblies()`.
- Don't use attribute-driven "register against every interface" scanning.
- Organize by feature inside each project (`Application/Orders/PlaceOrder/PlaceOrderCommand.cs`, `PlaceOrderCommandHandler.cs`, `PlaceOrderCommandValidator.cs`; `Api/Endpoints/Orders/...`), one type per file. Domain folders are per aggregate (`Domain/Orders/`).

## 3. Domain modeling
- Aggregates protect their invariants. Use `{ get; private set; }`, a private parameterless ctor for EF, and static factories or behavior methods. No public setters, no public parameterless ctors.
- Expose child collections as `IReadOnlyList<T>` over a `private readonly List<T>`. Mutate only through root methods. Child-entity ctors and mutators are `internal`.
- Derive computed state (totals, status) inside the aggregate. Never set it from outside.
- Reference other aggregates **by ID only**. No navigation properties across aggregates.
- Change **one aggregate per transaction**. Cross-aggregate consistency goes through domain events (§7).
- Keep aggregates small. Warning signs: many child collections, "include everything" loads, flags to skip children.
- No anemic models. Behavior lives in entities and domain services; handlers orchestrate.
- Every entity gets a strongly typed ID: `readonly record struct XxxId(Guid Value)` with `New()` using `Guid.CreateVersion7()`. Reject `default`/`Guid.Empty` at creation. Generate the ID in the factory, not in the caller.
- Value objects with invariants: `sealed record` with a private ctor, get-only properties and `static Result<T, Error> Create(...)`. Use `readonly record struct` only for IDs and invariant-free values, because `default(struct)` and `with` bypass validation.
- Value-object operations return new instances. No implicit conversions from primitives (e.g. no `implicit operator Money(decimal)`).
- Enums: use a plain `enum` for behavior-free values, persisted as a string. Promote it to a sealed smart enum (private ctor, static instances, static registry dictionary declared *before* the instances, `TryFromCode`) once a `switch` on it appears outside its aggregate or values need data/behavior. Persist an explicit stable code, never the display name.
- No `Ardalis.*`, `StronglyTypedId`, `ValueOf` or other packages in Domain. Hand-roll the small types.
- Time enters the domain as a parameter (`placedAt`), supplied by the handler from `TimeProvider`.
- Domain does not log.
- Guards vs Result:
  - Programmer errors / broken preconditions → BCL throw helpers (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfNegativeOrZero`, ...).
  - Business rules a user can trigger → return a failed `Result` with an `XxxErrors` member.
  - Never handle the same rule both ways.

```csharp
public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = [];

    public OrderStatus Status { get; private set; }

    public Result<Unit, Error> AddLine(ProductId product, int quantity, Money unitPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);   // bug if reached: validator owns input
        if (Status != OrderStatus.Pending)
        {
            return OrderErrors.NotPending;                              // expected business answer
        }

        _lines.Add(new OrderLine(product, quantity, unitPrice));       // internal ctor
        return Unit.Value;
    }
}
```

## 4. Application layer & CQRS
- Mediator: use the solution's **own** abstractions in `Application/Abstractions/Messaging`. Keep handler interfaces mediator-agnostic so the dispatcher can be swapped.
  - Default dispatcher: a small hand-rolled **scoped** `ISender` with cached delegates, or the source-generated `Mediator` package (verify license). Behaviors can also be decorators (Scrutor `Decorate`).
  - MediatR only if the org holds a license or qualifies for Community. v13+ is RPL-1.5/commercial; ≤12.5.0 is Apache-2.0 but frozen. Never reference MediatR types from Domain.
- Register the sender **scoped**, never singleton (scoped `DbContext` behind handlers).
- One handler per request. Requests are immutable `sealed record`s carrying only input (primitives/DTOs). Handlers map to strongly typed IDs and value objects.
- Naming: `<Verb><Noun>Command`, `Get<Noun>[By<X>]Query`, `<Request>Handler`, `<Request>Validator`, `<Noun>Response`.
- Every handler returns `Result<T, Error>` (`Result<Unit, Error>` for no value). Thread `CancellationToken` through every handler, behavior and dispatcher.
- **Command handlers**: load the aggregate through its repository → call domain methods → `await uow.SaveChangesAsync(ct)` **once**. No `Update()`, no explicit transaction.
- **Query handlers**: no repositories, no `IUnitOfWork`, no side effects. Project straight to response records via `IReadDbContext` (Application interface, Infrastructure implementation over no-tracking sets) or Dapper. Application may reference `Microsoft.EntityFrameworkCore` (not provider packages) for async LINQ operators; Domain may not.
- Don't call `ISender.SendAsync` from inside a handler. Extract shared logic into a domain or application service.
- Do uniqueness and other DB-backed checks in the handler, returning `ErrorType.Conflict`, backed by a DB unique index.
- Resource-level authorization (ownership, tenant, state) happens in the handler after loading the resource (§13).
- The marker interfaces below don't use their type parameter. If S2326 flags them (verify), propose an `.editorconfig` change rather than suppressing it.

```csharp
public interface ICommand<out TResponse>;               // TResponse is Result<T, Error>; `out` for S3246

public interface IQuery<out TResponse>;

public interface ITransactionalCommand;                 // opt-in marker, see §5

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken ct);
}

public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct);

    Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct);
}

public sealed record PlaceOrderCommand(Guid CustomerId, IReadOnlyList<OrderLineRequest> Lines)
    : ICommand<Result<Guid, Error>>;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<Result<OrderResponse, Error>>;
```

## 5. Pipeline behaviors
- Registration order is execution order; the first registered is outermost. Keep exactly this order:
  1. **Logging/metrics/tracing**: one span per request type, a duration histogram, start/end logs and a warning when slow (all `[LoggerMessage]`). Log a failed `Result` at the level its `ErrorType` warrants and set the span status. **Don't catch exceptions here**: catching to log and rethrow breaks S2139. The ASP.NET Core span records them and `GlobalExceptionHandler` logs them once. Never log the request payload.
  2. **Validation**: run all `IValidator<TRequest>` **sequentially**. If any fail, return `TResponse.Failure(ValidationError)` (via `IFailable<TResponse>`, §12). Don't throw.
  3. **Authorization** (only if the solution authorizes per request).
  4. **Query caching**: only for requests implementing `ICachedQuery`.
  5. **Transaction**: only for `ITransactionalCommand`.
- Scope a behavior with an `is` check at the top of `HandleAsync` that returns early (`if (request is not ICachedQuery query) { return await next(ct).ConfigureAwait(false); }`, braces on their own lines). Don't use generic constraints that some requests can't satisfy.
- Query caching: `HybridCache.GetOrCreateAsync(key, factory, ...)` with the handler passed as the factory (never `await next()` evaluated first). Keys include every discriminating parameter **plus tenant and user**. Never cache failures. Never cache commands. Invalidate by tag from domain-event handlers. Cache DTOs, never entities.
- Transaction behavior (only for commands needing several `SaveChanges`, raw SQL or `ExecuteUpdate`): run inside `db.Database.CreateExecutionStrategy().ExecuteAsync(...)`, begin, `next`, and commit **only on success**. Rethrow exceptions, never swallow them. Never also call `SaveChanges` in the handler for the same work.
- Exception → HTTP mapping is not a behavior. It belongs to `GlobalExceptionHandler` (§12).

```csharp
public delegate Task<TResponse> HandlerDelegate<TResponse>(CancellationToken ct);

public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    Task<TResponse> HandleAsync(TRequest request, HandlerDelegate<TResponse> next, CancellationToken ct);
}

services.AddScoped<ISender, Sender>();
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(QueryCachingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
services.AddValidatorsFromAssembly(typeof(IApplicationMarker).Assembly, includeInternalTypes: true);
```

## 6. Persistence (EF Core 10)
### Repositories & Unit of Work
- At most one repository per **aggregate root**; never one per table. Interface in Domain next to the aggregate; implementation `internal sealed` in Infrastructure.
- Repositories expose intention-revealing loaders that `Include` the whole aggregate (`GetByIdAsync(OrderId, ct)`, `GetByIdsAsync`), plus `Add` and `Remove`. No `SaveChanges`, `Update`, `GetAll` or `IQueryable` returns. No public open-generic `IRepository<T>`, no `UnitOfWork.Repository<T>()`; an internal base class constrained to aggregate roots is fine.
- `IUnitOfWork { Task<int> SaveChangesAsync(CancellationToken ct); }` lives in Application and is implemented by `AppDbContext`. Register it as `static sp => sp.GetRequiredService<AppDbContext>()`.
- `DbContext`, repositories and UoW are **scoped**. Never let a repository or UoW dispose the `DbContext`.
- No generic CRUD services (`IGenericService<TEntity, TDto>`) and no mapping DTOs onto entities. One handler per use case.
- Never bind or return entities at the API. Use request/response records.

### Queries, specifications & pagination
- Read path: filter → order → page → `Select` into a record, all on `IQueryable`, then one terminal async call with `ct`. A DTO projection needs no `AsNoTracking()`.
- Keep EF's default tracking. Don't set global `NoTracking`, because "load → mutate → SaveChanges" would silently persist nothing.
- Never call `AsEnumerable()`, `ToList()` or `Compile()` before filtering/paging. No untranslatable methods in `Where`/`OrderBy`. No `StringComparison` overloads in queries; use collation or `EF.Functions.Like`.
- Capture scalars in lambdas so EF parameterizes them. Never build trees with changing `Expression.Constant`s.
- Use `AsSplitQuery()` explicitly on queries that `Include` 2+ collections. Keep the single-query default; don't default to split.
- Specifications are optional; introduce them only when a repository's loaders multiply, never for single-use queries. A spec holds criteria (`Expression<Func<T,bool>>`) and includes only; ordering and paging live in the query envelope. Combine specs with an `ExpressionVisitor` that rebinds parameters, not `Expression.Invoke`.
- List endpoints accept data, not code. Sort is an allowlisted enum mapped by `switch` to typed `OrderBy...ThenBy(x => x.Id)`; unknown sort → 400. Never reflection/string `OrderBy` over arbitrary properties.
- Every paged ordering is fully unique (PK as tie-breaker). Page size default 20, max 100; reject `> max` or `page < 1` with 400; compute the offset with `checked`.
- Default to **keyset** pagination for large tables, feeds and audit logs: `Where(k < c.k || (k == c.k && id < c.id))`, `Take(n + 1)` for `HasMore`, an index matching filter + order. Offset only when numbered pages are a real requirement. Cursors are opaque (Base64Url), untrusted (malformed → 400) and bound to sort mode + filter version.
- Total count is opt-in, a separate sequential `CountAsync`. Never run it in parallel on one context.
- Guid keyset ordering on SQL Server differs from .NET ordering (verify per provider). Prefer v7 GUIDs or sequential keys.

### Configuration & conventions
- One `internal sealed class XxxConfiguration : IEntityTypeConfiguration<Xxx>` per entity, loaded with `ApplyConfigurationsFromAssembly`. No data annotations on domain types.
- `AppDbContext` is modelling only: primary ctor `(DbContextOptions<AppDbContext> options)`, `DbSet<T> X => Set<T>()`, no `OnConfiguring`, no connection strings.
- Type-wide defaults go in `ConfigureConventions` (string max length, `decimal` precision, strongly typed ID converters via `Properties<OrderId>().HaveConversion<OrderIdConverter>()`).
- Map value objects with `ComplexProperty` (and `.ToJson()` for collections), not `OwnsOne`/`OwnsMany`, in new code. Keep owned types only where an existing schema needs them.
- Persist plain enums as strings with `HasMaxLength`. Back critical invariants with check constraints, `IsRequired` and `HasMaxLength`.
- The model never depends on environment or configuration values.
- One `AddPersistence(...)` in Infrastructure binds `DatabaseOptions` (validated on start) and configures the provider with `CommandTimeout` and `EnableRetryOnFailure()`.
  - `EnableSensitiveDataLogging`/`EnableDetailedErrors` only in Development.
- Provide an `IDesignTimeDbContextFactory<AppDbContext>` that reuses the runtime provider setup. Migrations live in Infrastructure.
- Seed with `UseSeeding` **and** `UseAsyncSeeding` (both are required). `HasData` only for fixed-key reference data. Never seed during normal multi-instance startup.
- Clustered GUID keys: sequential/v7, never `NEWID()`. Avoid DB-table counter or time-based key generators; use sequences or HiLo.

### Interceptors, auditing & soft delete
- Cross-cutting save logic (audit stamps, soft delete, tenant stamping, domain events → outbox) goes in `SaveChangesInterceptor`s, **one per concern**. Not in handlers, repositories or a `SaveChanges` override.
- Interceptors override **both** sync and async methods and null-check `eventData.Context`.
- Register interceptors through DI: `AddDbContext<AppDbContext>(static (sp, o) => o.AddInterceptors(sp.GetRequiredService<X>()))`. Stateless ones with singleton-safe dependencies are singletons; per-save state or scoped dependencies → scoped. Never call `BuildServiceProvider()` during registration.
- `IAuditable` (`CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy`) is stamped by an interceptor from `TimeProvider` and `IUserContext`; set `CreatedAtUtc`/`CreatedBy` `IsModified = false` on updates. Non-HTTP work uses an explicit system principal (`"system:<job-name>"`), never null.
- A full audit trail (old/new values) must commit atomically with the change. Use client-generated keys so audit rows join the same `SaveChanges`, or evaluate SQL Server temporal tables. Never use a second `SaveChanges` or a second `DbContext`.
- Soft delete is **opt-in per aggregate**. Ask first whether the "delete" is really a business state transition (cancel, void).
  - Where adopted: `ISoftDeletable { IsDeleted, DeletedOnUtc, DeletedBy }`, an interceptor converting `Deleted` to `Unchanged` and then stamping the columns (not `Modified`, which rewrites every column) (verify with an integration test), and a **named** filter `HasQueryFilter(QueryFilters.SoftDelete, e => !e.IsDeleted)`.
  - Add filtered indexes (`HasFilter("[IsDeleted] = 0")`), including on unique indexes.
  - Make the whole aggregate soft-deletable or use `DeleteBehavior.Restrict`, so dependents aren't hard-deleted by cascade.
- Every query filter is named (constants in `QueryFilters`); named and unnamed filters can't be mixed on one entity. Bypass only by name: `IgnoreQueryFilters([QueryFilters.SoftDelete])`. Parameterless `IgnoreQueryFilters()` is forbidden.

### Lifetime, transactions & bulk operations
- One scoped `DbContext` per request. Never share a context across threads, never `Task.WhenAll` over queries on one context, and no "thread-safe DbContext" wrappers.
- Parallel work, background services and extra units of work use `IDbContextFactory<AppDbContext>` (`await using`) or a new DI scope per task.
- `AddDbContextPool` only when measured, and only for a context with no scoped ctor dependencies or per-request state.
- One `SaveChangesAsync` is already a transaction. With `EnableRetryOnFailure`, every explicit `BeginTransactionAsync` runs inside `CreateExecutionStrategy().ExecuteAsync`, with a re-runnable delegate.
- `ExecuteUpdateAsync`/`ExecuteDeleteAsync` run immediately, outside the change tracker, interceptors and concurrency checks. Don't mix them with tracked changes in one unit of work; set audit/soft-delete columns yourself in `SetProperty`; include the concurrency token in `Where` and check the count; `ExecuteDelete` only for intended hard deletes. Whether global query filters apply to them (verify).
- No raw SQL against tenant-owned or soft-deletable tables outside a reviewed repository method.

## 7. Domain events
- Raise events inside aggregate behavior methods and factories via `protected Raise(...)`. Never raise them from handlers or endpoints.
- Events are immutable `sealed record`s in Domain, named in the **past tense** in business language (`OrderPaid`). Each carries `EventId` and `OccurredOnUtc` plus IDs and small values, never object graphs. No technical events (`EmailSent`) and no "god" events (`DataChanged(object)`).
- Domain defines its own `IDomainEvent`/`IHasDomainEvents`. No `INotification` or other framework types.
- A `SavingChangesAsync` interceptor collects events from `ChangeTracker.Entries<IHasDomainEvents>()`, writes them as outbox rows **in the same `SaveChanges`**, then clears them.
- Default: every handler with side effects runs **after commit from the outbox**, never as an in-process "publish after SaveChanges".
- Exception: same-database, no-I/O reactions that must be atomic may run in-transaction, dispatched before the base save (loop until no new events).
- `IDomainEventHandler<in TEvent>` (`HandleAsync(TEvent, CancellationToken)`) lives in Application (orchestration) or Infrastructure (external systems). Resolve each in its own scope, run handlers **sequentially**, make them idempotent, and never swallow their exceptions.
- Domain events never go on the wire. The outbox processor maps them to versioned integration-event contracts before publishing.

```csharp
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New()
    {
        return new OrderId(Guid.CreateVersion7());
    }
}

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }
}

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

// Concrete, not abstract: S1694 rejects abstract classes with no abstract members.
public class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : struct
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}

public sealed record OrderPaid(Guid EventId, DateTimeOffset OccurredOnUtc, OrderId OrderId) : IDomainEvent;
```

## 8. Outbox & Inbox
- Use the outbox wherever a state change must be followed by a message, email, webhook or other external effect. That removes the dual write. Idempotent APIs don't solve it.
- Outbox writes are automatic (interceptor, §7). Application code never touches the outbox table.
- `Type` is a stable registry name (`"orders.order-paid.v1"`) resolved through an explicit name ↔ type map. Never use `GetType().Name` + `Type.GetType`.
- The processor is a `BackgroundService`:
  - It creates a scope per batch through `IServiceScopeFactory`; never inject scoped services into its ctor.
  - It processes batches of 50–100 in `OccurredOnUtc` order and isolates failures per message. Catch only the specific exceptions the serializer and publisher throw (`catch (Exception)` breaks CA1031/S2221), record `LastError`, log once, and don't rethrow.
- **Claim rows** so instances can scale out: SQL Server `WITH (UPDLOCK, READPAST, ROWLOCK)`, PostgreSQL `FOR UPDATE SKIP LOCKED`, or a lease column set with `ExecuteUpdateAsync`. A bare `Where(ProcessedOn == null).Take(n)` double-publishes.
- Cap retries: `Attempts` + `NextAttemptOnUtc` (exponential backoff + jitter), then dead-letter after N attempts with `LastError`. Never retry forever.
- Use a filtered index on pending rows. Clean up processed rows in batches (7–30 days) from a separate job.
- Expose metrics: oldest-pending age, processed count, failures, batch size. Restore the stored trace parent when dispatching.
- Delivery is **at-least-once**, so every consumer is idempotent through the inbox.
- Inbox: PK `(MessageId, Consumer)`. Insert the inbox row and apply the effect in the **same `SaveChanges`**; a unique violation means the message was already applied, so acknowledge and return.
  - Never check-then-insert.
  - Detect duplicates by provider error code (SQL Server 2627/2601, PostgreSQL `23505`), not by message text.
- Out-of-order delivery: carry a monotonically increasing `Version` and ignore stale updates.
- Build vs buy: a hand-rolled EF outbox/inbox is fine for one service or a modular monolith. MassTransit v9 and NServiceBus are commercial. MassTransit's in-memory outbox is **not** an inbox. Record the choice in §1.

```csharp
public sealed class OutboxMessage
{
    public Guid Id { get; init; }                          // = IDomainEvent.EventId
    public required string Type { get; init; }             // registry name, never a CLR name
    public required string Payload { get; init; }          // JSON; jsonb on PostgreSQL
    public DateTimeOffset OccurredOnUtc { get; init; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }    // null = pending (filtered index)
    public int Attempts { get; set; }
    public DateTimeOffset? NextAttemptOnUtc { get; set; }
    public string? LastError { get; set; }
    public string? TraceParent { get; init; }              // Activity.Current?.Id at write time
}
```

## 9. Idempotency & webhooks
- Retryable side-effecting POSTs (payments, orders) require an `Idempotency-Key` header (IETF draft semantics, verify current revision): missing → 400, same key + different payload hash → 422, still in flight → 409, completed → replay stored status and body. Scope keys per client, publish a TTL (24 h–7 d), reserve atomically (insert `InProgress` first) and back with a unique constraint. A correlation ID is not an idempotency key.
- Webhook delivery is the outbox applied to HTTP. Never send or retry inline in a request (`Task.Delay` loops, `Task.WhenAll` fan-out).
- Sign with `HMACSHA256.HashData` over `{id}.{timestamp}.{rawBody}` using a per-subscriber vault secret (dual-secret rotation). Send `webhook-id`/`webhook-timestamp`/`webhook-signature`. The event ID stays stable across retries and DLQ replays.
- Only 2xx is success. One resilience pipeline, persisted `Retry-After`, per-subscriber circuit breakers, delivery log, DLQ with audited replay, ordering per subscriber only. SSRF guard: https only, resolve once, reject loopback/private/link-local/metadata ranges, pin the validated IP.
- Receiving: verify over the **raw body** (`EnableBuffering`) before deserializing, secret looked up server-side (never from a header), `CryptographicOperations.FixedTimeEquals`, ±300 s timestamp window, dedupe on the event ID via the inbox, 2xx fast and process asynchronously.

## 10. API layer
- Minimal APIs for new endpoints; add controllers only if the solution already uses them. One `IEndpoint` class per endpoint, discovered by assembly scan from the API marker and mapped once with `app.MapEndpoints(group)`.
- Endpoints are adapters: bind → `sender.SendAsync(...)` → map `Result` to `TypedResults`. No business rules, no `DbContext`, no try/catch, no entities in or out.
- Return `TypedResults` with `Results<...>` union return types. Don't use `[ProducesResponseType]` + `IncludeOpenAPIAnalyzers` (deprecated) or `.WithOpenApi()` (deprecated, use operation transformers).
- `.WithName(...)`, `.WithTags(...)`, `MapGroup` per resource for shared prefix, auth, rate-limit and timeout policy.
- Versioning: `Asp.Versioning.Http` (+ `Asp.Versioning.Mvc.ApiExplorer`), URL segment `api/v{version:apiVersion}`, an `ApiVersionSet` on the root group, `ReportApiVersions = true`, major-only versions.
  - Mark deprecated versions explicitly. Don't use `AssumeDefaultVersionWhenUnspecified`.
  - Version contracts and endpoints; map every version onto the same Application use case.
- OpenAPI: built-in `AddOpenApi()`/`MapOpenApi()` with one document per version, plus a UI (e.g. Scalar) **only in Development** or behind auth. Don't add Swashbuckle to a solution that doesn't use it.
  - Add security requirements per operation only where endpoint metadata requires auth.
- Responses: plain DTOs on success, `201 Created` + `Location` for creates, `204` for no content. Every error is RFC 9457 ProblemDetails (§12).
  - No `{ success, data, message }` envelope and no 200 with an error body.
  - A paged collection record (`items` + paging metadata) is fine.
- Request timeouts: `AddRequestTimeouts` with a default policy. The default timeout status is 504. The timeout only signals `RequestAborted`, so every endpoint takes a `CancellationToken` and passes it down.
- Output caching (`.CacheOutput()`) for public GETs, placed after auth. It doesn't cache authenticated requests by default; never cache per-user/tenant data without varying by them. No `UseResponseCaching` by default.
- Leave response compression to the reverse proxy if it already compresses.

```csharp
internal sealed class PlaceOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("orders", PlaceOrderAsync)
            .WithName("PlaceOrder")
            .WithTags("Orders")
            .RequirePermission(Permissions.Orders.Write);
    }

    private static async Task<Results<Created<Guid>, ProblemHttpResult>> PlaceOrderAsync(
        PlaceOrderRequest request,
        ISender sender,
        CancellationToken ct)
    {
        Result<Guid, Error> result = await sender.SendAsync(request.ToCommand(), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        // Uri overload, not string: CA2234.
        return TypedResults.Created(new Uri($"/api/v1/orders/{result.Value}", UriKind.Relative), result.Value);
    }
}
```

```csharp
app.UseForwardedHeaders();                   // behind a proxy: KnownProxies/KnownNetworks configured
app.UseExceptionHandler();                   // GlobalExceptionHandler + AddProblemDetails()
app.UseStatusCodePages();                    // empty 4xx/5xx get ProblemDetails
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();                    // project middleware, Response.OnStarting
app.UseRouting();
app.UseCors();                               // only if a browser origin needs it
app.UseAuthentication();
app.UseTenantResolution();                   // only if §14 applies
app.UseAuthorization();
app.UseRateLimiter();                        // after auth: partitions by client/user
app.UseRequestTimeouts();
app.UseOutputCache();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = static _ => false });
app.MapEndpoints(versionedGroup);
await app.RunAsync().ConfigureAwait(false);
```

## 11. Validation
| Kind | Where | How | Failure |
|---|---|---|---|
| Request/command shape (required, length, format, cross-field) | Application, one `AbstractValidator<TCommand>` per command | `ValidationBehavior` (§5) | `ValidationError` → 400 `ValidationProblemDetails` |
| Business rules / invariants | Domain factories and methods | `Result` + `XxxErrors` | 404/409/403 per `ErrorType` |
| State-dependent checks (uniqueness, existence) | Handler | Query + DB unique index | `ErrorType.Conflict` → 409 |
| Configuration | Startup | Options + `ValidateOnStart()` | App fails to boot |
| Raw JSON documents with a published schema | Infrastructure | JsonSchema.Net (`Evaluate`) | 400 |

- Validators are pure and synchronous: shape only, no repositories, no `MustAsync` DB calls. Attach stable error codes (`.WithErrorCode("Order.LinesRequired")`).
- One request-validation engine per solution. With FluentValidation in the pipeline, don't add .NET 10 `AddValidation()`, and don't use `FluentValidation.AspNetCore` auto-validation (deprecated).
- One command per use case with its own validator. No `RuleSet`s to share a DTO across create/patch.
- Use 400 for every validation error. Don't use 422 for validation (422 is reserved for idempotency-key payload mismatch, §9).
- Options pattern:
  - Bind with `AddOptions<T>().BindConfiguration(T.SectionName).ValidateDataAnnotations()` (or an `[OptionsValidator]` source-generated validator) `.ValidateOnStart()`. Never use `Configure<T>(GetSection(...))` without validation.
  - Options classes are `sealed` with `public const string SectionName` and `{ get; init; }` properties (get-only properties don't bind).
  - Inject `IOptions<T>` by default and `IOptionsMonitor<T>` in singletons that need reload. Never inject `IOptionsSnapshot<T>` into singletons. Never read raw `IConfiguration["A:B"]` inside services.
  - Use a FluentValidation `IValidateOptions<T>` adapter (singleton, sync rules only) only for complex config rules.
  - `ValidateOnStart` checks config shape. DB reachability belongs to readiness health checks, not startup.

```csharp
public sealed class PaymentGatewayOptions
{
    public const string SectionName = "PaymentGateway";

    [Required]
    [Url]
    public string BaseAddress { get; init; } = string.Empty;

    [Range(1, 60)]
    public int TotalTimeoutSeconds { get; init; } = 15;
}

builder.Services.AddOptions<PaymentGatewayOptions>()
    .BindConfiguration(PaymentGatewayOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## 12. Error handling & Result
- Expected outcomes (validation, not found, conflict, forbidden, business rule) → `Result<T, Error>`. Exceptions → bugs, infrastructure failures, misconfiguration. This matches the global rule; it is not optional per feature.
- Use one hand-rolled, dependency-free `Error`/`Result<TValue, TError>` in Domain (or a SharedKernel Domain references). `TError` is `Error` unless a caller genuinely switches on a narrower subtype.
- Errors are static members of one `XxxErrors` class per aggregate, with stable namespaced codes (`"Order.NotPending"`). Descriptions are safe to show clients.
- Prefer an early return on failure, braced: `if (!result.IsSuccess) { return result.Error; }`, with each brace on its own line. Use `Match` at the edge. Add `Map`/`Bind` if useful. No LINQ query-syntax monads. Pure transforms that can't fail return plain `T`.
- Never catch broad exceptions and convert them to `Result` failures (`ex.Message` in an `Error`). Let them reach the global handler.
- Don't define `NotFoundException`/`ValidationException` for normal flow.
- One API-layer mapping, `Error.ToProblem()`: Validation 400, Unauthorized 401, Forbidden 403, NotFound 404, Conflict 409, Failure 500. Add the `errorCode` extension.
- `GlobalExceptionHandler : IExceptionHandler`:
  - Registered with `AddExceptionHandler<GlobalExceptionHandler>()` + `AddProblemDetails()` + `app.UseExceptionHandler()`.
  - **Logs the exception itself**: since .NET 10 the middleware no longer logs exceptions a handler handled.
  - Writes through `IProblemDetailsService` with a generic title, never `ex.Message` or a stack trace.
  - Keeps `BadHttpRequestException.StatusCode`.
- `AddProblemDetails(static o => o.CustomizeProblemDetails = ...)` adds `traceId` (`Activity.Current?.Id ?? TraceIdentifier`) and `instance` to every problem.
- Log 5xx at Error and expected 4xx at Warning or lower, once, centrally. No logging in every catch block.

```csharp
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
}

public record Error(string Code, string Description, ErrorType Type);

public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation);

public static class OrderErrors
{
    public static readonly Error NotPending = new("Order.NotPending", "Only pending orders can change.", ErrorType.Conflict);

    public static Error NotFound(OrderId id)
    {
        return new Error("Order.NotFound", $"Order '{id.Value}' was not found.", ErrorType.NotFound);
    }
}

public readonly record struct Unit
{
    public static readonly Unit Value;                  // `= new()` would break SA1129
}

public interface IFailable<TSelf>
    where TSelf : IFailable<TSelf>
{
    bool IsSuccess { get; }

    static abstract TSelf Failure(Error error);
}
```

```csharp
public sealed class Result<TValue, TError> : IFailable<Result<TValue, TError>>
    where TError : Error
{
    private readonly TValue? _value;
    private readonly TError? _error;

    private Result(TValue? value, TError? error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }

    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Failed result has no value.");

    public TError Error => !IsSuccess ? _error! : throw new InvalidOperationException("Successful result has no error.");

    public static implicit operator Result<TValue, TError>(TValue value) => FromTValue(value);

    public static implicit operator Result<TValue, TError>(TError error) => FromTError(error);

    public static Result<TValue, TError> Success(TValue value)    // needed when TValue is an interface
    {
        return new Result<TValue, TError>(value, error: null, isSuccess: true);
    }

    public static Result<TValue, TError> Failure(Error error)
    {
        if (error is not TError typed)
        {
            throw new InvalidCastException($"Error '{error.Code}' is not a {typeof(TError).Name}.");
        }

        return new Result<TValue, TError>(value: default, error: typed, isSuccess: false);
    }

    // CA2225 named alternates for the implicit operators (verify: the CA2225 message names the exact methods it wants).
    public static Result<TValue, TError> FromTValue(TValue value)
    {
        return Success(value);
    }

    public static Result<TValue, TError> FromTError(TError error)
    {
        return new Result<TValue, TError>(value: default, error: error, isSuccess: false);
    }
}
```

```csharp
public static ProblemHttpResult ToProblem(this Error error)
{
    if (error is ValidationError validation)
    {
        return TypedResults.Problem(new HttpValidationProblemDetails(validation.Errors.ToDictionary(StringComparer.Ordinal))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = validation.Description,
            Extensions = { ["errorCode"] = validation.Code },
        });
    }

    int status = error.Type switch                     // every member listed (IDE0072) plus `_` (CS8509)
    {
        ErrorType.Failure => StatusCodes.Status500InternalServerError,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };

    return TypedResults.Problem(new ProblemDetails
    {
        Status = status,
        Title = error.Description,
        Extensions = { ["errorCode"] = error.Code },
    });
}
```

## 13. Authentication & authorization
- Users authenticate through an OIDC identity provider. No hand-rolled login/password endpoints, no locally minted JWTs with keys in config.
- JWT bearer: `Authority` + `Audience` from config, `MapInboundClaims = false`, `NameClaimType = "sub"`, `RoleClaimType = "role"`. Never `ValidateAudience = false`, never a hard-coded or fallback signing key.
- Machine clients: OAuth2 client-credentials (scopes), or mTLS where infrastructure supports it. API keys identify an *application*, for partner/automation clients.
- Deny by default: `AddAuthorizationBuilder().SetFallbackPolicy(RequireAuthenticatedUser)` and opt out with `AllowAnonymous()`.
- Authorize by **permission**, not role; roles are data mapping to permissions. Permissions are constants (`Permissions.Orders.Read = "orders:read"`) grouped per bounded context.
- One declarative marker per endpoint: `[HasPermission(...)]` implementing `IAuthorizationRequirementData`, or a `.RequirePermission(...)` extension adding a `PermissionRequirement`. No hundreds of pre-registered policies and no string-parsing policy providers (except for any/all composites).
- `AuthorizationHandler<TRequirement>`: one concern each, composable. Call `context.Succeed`; **never** call `context.Fail()` for "not satisfied". Admin/override logic lives in one handler or a `*` permission, not in `IsInRole("Admin")` checks.
- Stacking markers on group and endpoint means **both** are required.
- Resource-based checks (ownership, record-level rules) run in the Application handler after loading the resource, via `IAuthorizationService.AuthorizeAsync(user, resource, requirement)`. Return `NotFound` when the caller must not learn the resource exists (cross-tenant), `Forbidden` otherwise.
- Every service enforces its own authentication and authorization. A gateway (YARP, not Ocelot) may add coarse checks but never replaces them.
- Tokens stay small: `sub`, audience, scopes (+ `client_id`). Resolve permissions server-side per (user, tenant) in one **idempotent** `IClaimsTransformation` (only one runs): return early on a marker claim, add a **separate** `ClaimsIdentity`, cache in `HybridCache` keyed `perm:{tenant}:{sub}`, invalidate by tag on role change, fail closed.
- Read identity through `ICurrentUser` (Application port; Api implementation). No `FindFirst("...")` string lookups scattered in code.
- API keys: an `AuthenticationHandler<ApiKeyOptions>` producing a `ClaimsPrincipal` (`client_id`, `key_id`, scopes, tenant), never middleware or filters.
  - Header only. CSPRNG (≥32 bytes), prefixed (`ak_live_`/`ak_test_`), shown once. Store only `HMAC-SHA256(pepper, secret)` (pepper from the vault); compare with `CryptographicOperations.FixedTimeEquals`.
  - Absent → `NoResult()`, invalid → `Fail()` (401). Expiry, revocation, 2-key rotation, last-used tracking via a queue (not `Task.Run`). Cache by key ID, never `GetHashCode()`. Log the key ID, never the key.
- Signed requests (if needed) use a separate per-client secret over method, path, timestamp, nonce and body hash, with replay protection.
- Security baseline: HTTPS + HSTS outside Development; Kestrel `AddServerHeader = false`; headers via `Response.OnStarting` (`nosniff`, `Referrer-Policy: no-referrer`, `Cache-Control: no-store` on non-public responses, CSP `default-src 'none'; frame-ancestors 'none'` for JSON; never `X-XSS-Protection: 1`).
- CORS only for real browser clients, explicit origin allowlist, never `AllowAnyOrigin()` with credentials. Built-in rate limiter partitioned by `client_id`/`sub` with IP fallback and `RejectionStatusCode = 429` (default is 503); no hand-rolled limiters.
- Secrets live in a vault (user-secrets in dev), never in `appsettings*.json` or source. Log authorization failures as security events, without tokens.

## 14. Multi-tenancy (optional — delete if single-tenant)
- Tenant ≠ user. `ITenantContext` (Application) is an ambient context parallel to `ICurrentUser`.
  - A swappable resolution strategy (host/subdomain, header or route; claim only when the IdP issues tenant-scoped tokens) populates it in middleware.
  - Jobs, consumers and impersonation set it through `ITenantContextSetter`.
- A client-supplied tenant is untrusted. Authorize **membership** of the principal in that tenant. API keys are bound to one tenant.
- Unresolved tenant → fail closed with 400/404 ProblemDetails. Never default to all tenants.
- Data access depends only on `ITenantContext`, never on claims or `HttpContext`.
- EF isolation:
  - A **named** tenant filter applied by convention to every `ITenantOwned` entity. It references a `DbContext` member (`e.TenantId == CurrentTenantId`), never `Expression.Constant(service)`.
  - A `SaveChangesInterceptor` stamps `TenantId` on insert and **throws** if it is modified.
  - `TenantId` leads composite indexes. Exclude reference data from the convention.
- Cache keys, output-cache variation and permission caches include the tenant ID.
- Database-per-tenant: resolve the connection from a cached platform lookup. Provide a design-time factory.

## 15. Logging & observability
- Inject `ILogger<T>` everywhere. Serilog is the provider, configured only in `Program.cs`: two-stage bootstrap, `ReadFrom.Configuration`, `Log.CloseAndFlushAsync()`. No `Serilog.*` types or `LogContext` in Domain or Application.
- Levels, sinks and overrides live in `appsettings.{Env}.json` (Default `Information`; `Microsoft`, `Microsoft.AspNetCore`, `System` → `Warning`).
- Output: JSON (compact) outside Development, to stdout in containers. Wrap file/network sinks in `WriteTo.Async`.
- One request log per HTTP request (`UseSerilogRequestLogging`): Error on exception/5xx, Warning when > 1 s, health endpoints excluded. Never `HttpLoggingFields.All`.
- **Every** log call is a `[LoggerMessage]` source-generated method in a per-feature `internal static partial class XxxLog` (CA1848). Never call `logger.LogInformation(...)`/`LogError(...)` directly. Redaction also only works through these methods.
- Message templates are constant, with PascalCase placeholders and stable names (`{OrderId}`) (CA2254, S6678). Pass the exception as an `Exception` parameter of the log method, never `ex.Message` in the template.
- In a `catch`, either log or rethrow, never both (S2139).
- Never log secrets, tokens, API keys, connection strings, `Authorization` headers or request/response bodies. Log surrogate IDs, never PII.
  - No `{@Entity}` destructuring of entities or DTOs. Use `[TagProvider]` to choose properties without attributing domain types.
- Redaction: one classification taxonomy (`[PersonalData]`, `[SensitiveData]`) in a low-level project; call both `builder.Logging.EnableRedaction()` **and** `builder.Services.AddRedaction(...)`; pass explicit classifications to every `SetRedactor`; annotate the `[LoggerMessage]` parameter itself. Serilog owning the logger factory may bypass `EnableRedaction`/`EnableEnrichment` (verify with a spike before relying on redaction).
- Per-operation context via `logger.BeginScope(...)` inside `using`. Static context (service, version, environment) via enrichers or OTel resource attributes.
- OpenTelemetry: `AddOpenTelemetry().ConfigureResource(r => r.AddService(name, serviceVersion))` with ASP.NET Core, HttpClient, runtime and EF Core instrumentation (EF package is prerelease); `.UseOtlpExporter()` once, endpoint from `OTEL_EXPORTER_OTLP_ENDPOINT`, to an **OpenTelemetry Collector**. No vendor or Jaeger exporters in the app.
- Logs reach OTLP by exactly one path: `Serilog.Sinks.OpenTelemetry` **or** Serilog `writeToProviders` + `WithLogging()`, never both (verify which the solution uses).
- One `static readonly ActivitySource` (+ `Meter`) per component, registered with `.AddSource(...)` or its spans are dropped. Spans for I/O, use-case handlers (LoggingBehavior) and significant work, not every method. Low-cardinality names (`"Pricing.Resolve"`), IDs in tags, no PII in tags or names. Always `activity?.`; on failure `SetStatus(ActivityStatusCode.Error, ...)` + `AddException(ex)`.
- Sampling: the app keeps the default parent-based always-on sampler. The Collector does tail sampling (errors, latency, baseline) and has a `batch` processor. Don't combine app head sampling with tail sampling.
- Correlation: `Activity.Current.TraceId` (W3C `traceparent`) is the key in logs, traces and ProblemDetails. Add `X-Correlation-Id` only if callers must supply one: validate it (≤128 chars, never `Guid.Parse` untrusted input), store it as Activity tag/Baggage + log scope, echo it, register it first. Never in `HttpContext.Items`.
- No fire-and-forget `_ = Task.Run(...)`. Enqueue to a `Channel`/`BackgroundService`, capturing `Activity.Current?.Context` as the consumer span's parent.
- Health checks: `/health/live` (no checks) and `/health/ready` (tag `ready`: own DB via `AddDbContextCheck<AppDbContext>`, broker, cache). No third-party APIs, self-URLs or memory thresholds in readiness; checks honor `CancellationToken` and have timeouts. Public endpoints are status-only; details go on a separate `.RequireAuthorization()` endpoint. Prefer built-ins over Xabaril/HealthChecks UI (unsupported by Microsoft).
- MiniProfiler is dev-only, never exposed without auth.

```csharp
internal static partial class OrderLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} placed with {LineCount} lines")]
    public static partial void OrderPlaced(this ILogger logger, Guid orderId, int lineCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Payment capture failed for {OrderId}")]
    public static partial void PaymentCaptureFailed(this ILogger logger, Exception exception, Guid orderId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sign-in failed for {Email}")]
    public static partial void SignInFailed(this ILogger logger, [PersonalData] string email);
}

// do:    logger.OrderPlaced(order.Id.Value, order.Lines.Count);
// do:    logger.PaymentCaptureFailed(exception, orderId);
// don't: logger.LogInformation("Order {OrderId} placed", id) (CA1848); $"..." templates; "{@Order}" destructuring
```

## 16. HttpClient & resilience
- Outbound dependencies are typed clients in Infrastructure behind an Application port: `AddHttpClient<IPaymentGateway, PaymentGatewayClient>(...)`, with the base address from validated options.
- Exactly **one** resilience handler per client: `.AddStandardResilienceHandler(o => ...)`. Set `TotalRequestTimeout`/`AttemptTimeout` there and leave `HttpClient.Timeout` at or above the total (verify).
  - Never add Polly v7 (`Microsoft.Extensions.Http.Polly`, `AddPolicyHandler`) or a hand-rolled retry loop on top.
  - Retry only idempotent operations; disable retries for unsafe methods where needed (`DisableForUnsafeHttpMethods`, verify).
- Don't inject typed clients into singletons. Use `IHttpClientFactory` per operation, or `PooledConnectionLifetime` for long-lived clients.
- Never dispose factory clients. Never mutate `DefaultRequestHeaders`/`BaseAddress` after creation; per-request headers go on `HttpRequestMessage`.
- Check the status (or read ProblemDetails) before deserializing. Use `System.Net.Http.Json` helpers, not `ReadAsAsync`/Json.NET.
- Stream large bodies (`ResponseHeadersRead` + `ReadFromJsonAsync`, `using var response`). Pass `ct` to every call.
- Pass `Uri`, not `string`, to `HttpClient` methods and any API with a `Uri` overload (CA2234). Properties and parameters named `*Url`/`*Uri` are typed `Uri` (CA1054, CA1056).
- Bound fan-out (`Parallel.ForEachAsync` with `MaxDegreeOfParallelism`), never `Task.WhenAll` over thousands of calls.
- `DelegatingHandler`s are transient, stateless and read context from `Activity.Current`.
  - Never propagate the user's `Authorization` header downstream; use token exchange/client credentials.
  - Never disable TLS validation. Never cache `HttpResponseMessage`; cache DTOs.
- HttpClient logging: `AddExtendedHttpClientLogging()` (+ `AddRedaction()`), with no bodies, headers or query strings in production.
- Tests stub the primary `HttpMessageHandler`. Never remove the factory registration in `WebApplicationFactory`.

## 17. Time
- Inject `TimeProvider` (registered `AddSingleton(TimeProvider.System)`), call `GetUtcNow()`. No `DateTime.Now/UtcNow`, `DateTimeOffset.Now/UtcNow`, custom `IDateTimeProvider` or time delegates.
  - Enforce it with `Microsoft.CodeAnalysis.BannedApiAnalyzers` + `BannedSymbols.txt`.
- Instants are `DateTimeOffset` (UTC, `...Utc` names). Use `DateOnly`/`TimeOnly`/`DateTime` only for wall-clock values; store an IANA zone ID when the zone matters.
- Domain receives time as a parameter. Interceptors and handlers take `TimeProvider`. Elapsed time uses `GetTimestamp()`/`GetElapsedTime()`.
- Tests use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`).

## 18. Tooling & build
- Root `Directory.Build.props`: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `AnalysisLevel=latest`, `TreatWarningsAsErrors`, `CodeAnalysisTreatWarningsAsErrors`, `EnforceCodeStyleInBuild`.
  - `AnalysisMode`: keep the solution's current level (start at `Recommended` and ratchet up). Don't jump to `All` in a mature codebase in an unrelated change.
- Central Package Management: `Directory.Packages.props` with `ManagePackageVersionsCentrally=true`, versionless `PackageReference`s, and `VersionOverride` only as a documented exception.
  - Analyzers (StyleCop.Analyzers, Roslynator, Meziantou, SonarAnalyzer, SecurityCodeScan, xunit.analyzers in tests) go in as `<GlobalPackageReference>`.
- The root `.editorconfig` (`root = true`) is the single source of style and analyzer severities. It sets `dotnet_analyzer_diagnostic.severity = error`, so every enabled rule fails the build. `GenerateDocumentationFile` must stay `true` (it is required for IDE0005, and it is what makes CS1591 fire).
  - Thresholds to stay under: methods ≤ 60 lines / 40 statements (MA0051), cognitive complexity ≤ 15 (S3776), nesting ≤ 4 (S134).
  - Change it only through a reviewed PR to the `.editorconfig` itself. Never work around it in code.
- CI runs `dotnet format --verify-no-changes`. CSharpier only if the solution already uses it.
- Pin repo tools in `.config/dotnet-tools.json` and the SDK in `global.json`. The solution file is `.slnx`.
- No `#pragma warning disable` or `[SuppressMessage]` at all (S1309). `!` (null-forgiving) needs a provable reason; prefer guards or correct annotations. Never use either to get a build green.
- Ordinal comparisons for identifiers (`StringComparison.Ordinal[IgnoreCase]`), never `ToLower()`/`ToUpper()` + `==`.
- Dispose only what you create. Seal disposable types; no finalizers unless you own an unmanaged handle (`SafeHandle`).
- `EnablePackageValidation` + baseline version only for libraries packed as NuGet packages, not for the API.
- Don't glob-load config files. Keep the default host configuration order.

## 19. Testing & architecture tests
- Follow the baseline testing rules (§0). Existing test libraries win.
- The `.editorconfig` applies to test projects too:
  - Test names have no underscores (CA1707): `HandleAsyncWhenCustomerHasNoLinesReturnsValidationError`.
  - Public test classes and methods need a one-line `/// <summary>` (CS1591).
  - Awaits use `.ConfigureAwait(true)` (CA2007 + xUnit1030).
  - Every test asserts something (S2699). No `Thread.Sleep` (S2925); use `FakeTimeProvider`.
- Domain: plain unit tests on aggregates and value objects, with no DB. Assert state, `Result` errors and raised events.
- Handlers: unit tests with substituted ports + `FakeTimeProvider`. Authorization handlers: build `AuthorizationHandlerContext` directly and assert `HasSucceeded`.
- API: `WebApplicationFactory<Program>` + Testcontainers on the production provider. Never EF InMemory or mocked `DbSet`/`IQueryable`, which hide missing commits and untranslatable queries.
- Query tests: every allowed sort/filter shape runs on the real provider. Assert deterministic order with duplicate sort keys, no overlap between keyset pages, and round-trip counts where it matters.
- Interceptors (soft delete, audit, tenant guard) and outbox/inbox duplicate handling get integration tests.
- Architecture tests (NetArchTest.Rules 1.3.2, or the maintained `NetArchTest.eNhancedEdition` fork) fail the build when: Domain depends on `<SolutionName>.Application`/`.Infrastructure`/`.Api`, `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`, `MediatR` or `FluentValidation`; Application depends on Infrastructure/Api; Infrastructure depends on Api; entities have public setters; handlers are unsealed or outside Application; an `ICommand<>` lacks an `IValidator<>`.
- Use full root namespaces in `HaveDependencyOnAny` (prefix match) and assert with `Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []))` (verify member name). Encode only rules you'd reject a PR over.

## 20. Never do
- [ ] Reference EF Core, ASP.NET Core, MediatR, FluentValidation or any package from Domain
- [ ] Put use-case handlers in Infrastructure or business rules in endpoints
- [ ] Return or bind entities at the API; return a success/fail envelope or 200 with an error body
- [ ] Add a second Result type, mediator, validation engine, logging provider or error shape
- [ ] Throw exceptions for expected outcomes, or wrap infrastructure exceptions in `Result` failures
- [ ] Return `ex.Message`, stack traces, SQL or request bodies to clients
- [ ] Call `SaveChanges` in repositories, add `Update()`, return `IQueryable`, or build generic CRUD repositories/services
- [ ] Set global `NoTracking`, share a `DbContext` across threads, or `Task.WhenAll` on one context
- [ ] Use `BeginTransaction` with retries enabled outside `CreateExecutionStrategy()`
- [ ] Call parameterless `IgnoreQueryFilters()` or write unnamed query filters
- [ ] Publish domain events in-process after commit for side effects, or put Domain types on the wire
- [ ] Poll the outbox without row claiming, retry forever, or resolve types with `Type.GetType(shortName)`
- [ ] Check-then-insert for inbox or idempotency records
- [ ] Sort or filter by client-supplied property names via reflection; page without a unique tie-breaker or a max size
- [ ] Interpolate log messages, log secrets/tokens/PII, or destructure entities with `{@...}`
- [ ] Use `DateTime.Now`/`UtcNow` or a custom clock interface
- [ ] `new HttpClient()` per call, stack resilience handlers, or disable TLS validation
- [ ] `_ = Task.Run(...)` fire-and-forget with scoped services
- [ ] Inject scoped services into singletons, middleware constructors, hosted-service constructors or `DelegatingHandler`s
- [ ] `ValidateAudience = false`, hard-coded signing keys, API keys in query strings/config/logs, `context.Fail()` for "not satisfied"
- [ ] Rely on gateway-only authorization or a JWT `tid` claim as the only tenant boundary
- [ ] Use deprecated APIs: `.WithOpenApi()`, `IncludeOpenAPIAnalyzers`, `Microsoft.AspNetCore.Mvc.Versioning`, `Microsoft.Extensions.Http.Polly`, `FluentValidation.AspNetCore` auto-validation, `HMAC.Create(string)`, Jaeger exporter
- [ ] Suppress an analyzer in code (`#pragma`, `[SuppressMessage]`, `GlobalSuppressions.cs`)
- [ ] Call `logger.LogXxx(...)` directly instead of a `[LoggerMessage]` method, or log and rethrow in the same `catch`
- [ ] Use `var` when the type isn't apparent, `await` without `ConfigureAwait`, or omit braces on a single-line `if`
- [ ] Put more than one type in a file, or leave a public member without an XML doc
- [ ] Silently "fix" code that deviates from this file — flag it instead
