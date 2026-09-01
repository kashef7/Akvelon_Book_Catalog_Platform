# Design Notes

> This is a living document. It gets extended every week as the project grows.

---

# Week 1

## What I built

A REST API for a book catalog with full CRUD (create, read all, read by id, update, delete), plus separate endpoints to update a book's status and rating.

Data is validated on the way in, Swagger is available to explore the API, key operations and errors are logged, and every endpoint returns a consistent response shape whether it succeeds or fails.

## How it's structured

The solution is split into three projects:

- **App_DAL**
  - The `Book` entity
  - An in-memory repository (`InMemoryBookRepo`) behind an `IBookRepo` interface.

- **App_BLL**
  - DTOs
  - AutoMapper profiles
  - `BookService`, which holds all the business rules and logging.

- **App_PL**
  - ASP.NET Core Web API project
  - Controllers
  - Swagger
  - Global exception handler

Dependencies only flow one way:

**`App_PL` → `App_BLL` → `App_DAL`**

The controller never talks to the repository directly, and it never touches the `Book` entity — it only knows about DTOs.

## Decisions I made, and why

### `Result<T>` instead of exceptions for expected outcomes

Things like "book not found" or "date published is in the future" aren't bugs, they're normal outcomes a client can trigger on purpose.

Throwing an exception for that felt like the wrong tool, so the service returns a `Result` object (success/failure + message + status code) instead, and the controller just maps it to the right HTTP response.

Exceptions are reserved for things that are genuinely unexpected (a real bug, a dependency failing) and those get caught once, centrally, by the `GlobalExceptionHandler`.

### Three separate class-library projects instead of folders in one project

This was a deliberate choice to make the layering hard to accidentally break.

A folder boundary is easy to ignore; a project boundary shows up as a compile error if I try to jump layers I shouldn't jump into.

*(Turns out this isn't airtight — see "what I found hard" below.)*

### In-memory storage with `ConcurrentDictionary` instead of a real database

A real database is out of scope until week 3, but I still wanted the storage to behave correctly under concurrent requests, so I used `ConcurrentDictionary<Guid, Book>` instead of a plain `List<Book>`/`Dictionary`, which isn't safe to write to from multiple requests at once.

### Soft delete instead of removing records

`DeleteBook()` just flips an `IsDeleted` flag.

Keeps history around, and it's the same pattern a real database version of this would probably use (a `DeletedAt` column instead of an actual `DELETE`).

### DTOs for every direction, mapped with AutoMapper

The controller never accepts or returns the `Book` entity directly.

- `BookCreateDto` / `BookEditDto` define exactly what a client is allowed to send in.
- `BookGetDto` defines exactly what they're allowed to see back.

This stops accidental overposting (a client setting `IsDeleted` on create, for example) and means the entity's internal shape can change without breaking the API contract.

AutoMapper handles the entity ↔ DTO conversion so I'm not hand-writing the same mapping code five times.

### Interfaces for the repository and the service (`IBookRepo`, `IBookService`)

Both exist so the layer above never depends on a concrete class, only a contract.

Concretely:

- The controller is constructed against `IBookService`, not `BookService`.
- `BookService` is constructed against `IBookRepo`, not `InMemoryBookRepo`.

This is what makes swapping `InMemoryBookRepo` for an EF Core repository in week 3 a one-line change in `Program.cs` instead of a rewrite of the service or controller.

### An abstract `AppException` base class instead of one custom exception per error type

A switch statement with one case per exception type doesn't scale — every new exception means editing the global handler.

Instead, any exception that derives from `AppException` carries its own `StatusCode`, so `GlobalExceptionHandler` only ever needs two branches:

1. "It's an `AppException`, use its status code."
2. "It's not, default to 500."

Adding a new known exception later costs nothing at the handler level.

### Keeping the `Book` fields minimal for now

`Title`, `Description`, `AuthorName`, `DatePublished`, `Rating`, `Status`.

I kept `AuthorName` as a plain string instead of a separate `Author` entity on purpose — modeling authors as their own entity (with a one-to-many or many-to-many relationship) is a real design question, but not one week 1 asked for, so I deferred it rather than guessing at a relationship I'd probably have to redo anyway.

### `BookStatus` as `NotStarted` / `Started` / `Finished`

This makes the catalog track a personal reading progress, not just "does this book exist" — closer to a reading list than a library inventory.

I picked this because it gives the rating field somewhere meaningful to live (rating a book you haven't started doesn't make sense), and because a small, closed enum is easy to extend later without breaking existing data, unlike a free-text status field.

## What I'd improve with more time

- Right now the `Result` object carries an HTTP status code, which means a business-logic class is holding an HTTP concept. That's a layering leak the same way the entity issue below is — I'd move status-code mapping fully into the controller.

- Soft-deleted books stay in memory forever with no cleanup. Given more time I'd add an expiry + background cleanup job, or at least a manual purge endpoint.

- `GetAllBooksAsync` returns every book, every time — no paging yet. That's a week 2 task, but it's the most obvious thing that won't survive real data volume.

## What I found hard

The most useful thing I learned this week wasn't a topic on the list — it was discovering that my layering wasn't as strict as I thought.

`App_PL`'s `.csproj` only references `App_BLL`, not `App_DAL`. I assumed that meant the controller physically couldn't see the `Book` entity.

It turns out project references in .NET are transitive at compile time: since `App_BLL` references `App_DAL`, `App_PL` can see `App_DAL`'s public types too, even without referencing that project directly.

Nothing in my controller uses `Book` right now, but the compiler wouldn't stop future-me from doing it by accident.

The real fix (making DAL entities `internal` and exposing them to `App_BLL` only via `InternalsVisibleTo`) is a week 2 task, but the lesson — that a project reference in the `.csproj` isn't the same guarantee as "this layer literally cannot see that layer" — is one I want to remember for every project after this one.

## Getting ahead of myself, and the plan for week 2

When week 2's instructions arrived, I realized I'd already built a chunk of what they were asking for — the layering, the DTOs, the repository abstraction, the centralized error handling — without having studied the topics behind them properly first.

I built by instinct and by copying patterns I'd seen, not from a solid understanding of *why* each one is the right call.

The entity-leak issue above is the clearest evidence of that: I put the pieces in place that were supposed to prevent it (separate projects, DTOs, an `IBookRepo` abstraction) but didn't actually understand project-reference transitivity well enough to know I hadn't closed the gap.

So instead of moving straight to week 2's new features, my plan is to slow down first:

- Study SOLID
- Study the Repository pattern
- Study DTO mapping
- Go back through what I already built
- Check each piece against that understanding
- Start by actually fixing the entity leak
- Then add anything new like pagination, filtering, or tests

I'd rather have a smaller set of things I can fully explain than a larger set I built on instinct.

---

# Week 2

## What I built

Pagination and filtering on `GET /api/books`, a `Common` project to hold shared types that both `App_BLL` and `App_DAL` need, an expanded unit test suite covering `BookService`, filtering, pagination, and DTO validation, and a pass over error handling to make it fail loudly instead of silently when something's unmapped.

## How I split the layers, and why exactly this way

Week 1 already had three layers, but by week 2 I ran into a real problem:

`BookStatus` and `BookQuery` needed to be visible to *both* `App_BLL` and `App_DAL`, and neither layer is allowed to depend on the other in that direction (`App_DAL` can't reference `App_BLL` — that would point the dependency arrow backwards).

Putting `BookStatus` in `App_DAL` and having `App_BLL` reference it back would've worked mechanically, but it's the wrong relationship conceptually: `BookStatus` isn't a data-access concept, it's a domain concept both layers need to talk about.

### The fix

The fix was a fourth project, `App_Common`, that both `App_BLL` and `App_DAL` reference directly, and that references neither of them back.

It only holds things with no business logic and no storage logic attached:

- `BookStatus`
- `BookQuery`

This is Dependency Inversion in the literal sense: instead of one layer depending on a concrete type owned by another layer, both layers depend on a shared abstraction that belongs to neither.

It also cleaned up a smaller smell from week 1 — DTOs no longer need to reference `App_DAL` at all just to use the `BookStatus` enum, which is one less accidental path for the entity-leak problem to get worse.

## What my data access abstraction hides, concretely

`IBookRepo` hides two things from `BookService`:

1. **Where** the data lives.
2. **How** it's queried.

`BookService` never sees `ConcurrentDictionary`, never sees LINQ against the underlying store, and never sees anything database-specific.

It calls:

`GetAllBooksAsync(BookQuery)`

and gets back:

`(IReadOnlyList<Book>, int totalCount)`

— no information about *how* that list or that count was produced.

When week 3 swaps `InMemoryBookRepo` for an EF Core-backed one, `BookService` doesn't change, because it was never written against anything more specific than the interface.

The filtering itself lives in `App_DAL` (`ApplyQueryFilters` in `BookFilters.cs`), not in the service, for the same reason — "how do I filter this data" is a data-access question, not a business-logic question.

`BookService` only decides *what* to filter by (it passes the mapped `BookQuery` straight through); it doesn't know or care whether that's a LINQ `.Where()` over an in-memory collection or eventually a SQL `WHERE` clause.

## Pagination, and why I capped it instead of trusting the client

`BookQueryParams.PageSize` clamps to a `MaxPageSize` of 50 in the property setter itself, not in the controller or the service.

If a client asks for a page size of 10,000, they silently get 50 back instead of an error.

I went back and forth on this — a `BadRequest` felt more "correct" in a validation sense — but a client asking for too much data isn't really an invalid request the way a missing title is; it's a request the server is allowed to satisfy partially.

Clamping keeps the endpoint usable instead of forcing every client to know and respect a magic number, and it protects the server from a client (accidentally or not) asking for everything at once.

### `PagedResult<T>`

`PagedResult<T>` carries `TotalCount` alongside the page of items for the same reason week 1's self-check questions push on:

A client that only sees 10 items has no way to know if there are 11 more or 11,000 more, and can't build "page 3 of 9" UI without that number.

The cost of exposing it is just running `.Count()` on the filtered query before paging it — cheap against an in-memory collection, and something I'll need to specifically check the cost of once this is real SQL in week 3.

## Filtering, honestly

Filtering supports `Title`, `Status`, and `Rating` right now, and `Title` is an exact match, not a partial/contains search.

I know that's a weak search experience — a client can't find "Dune" by typing "dun" — but I'd rather ship an honest exact-match filter I understand the cost of than a `Contains()` call I haven't thought through against a real database yet (case sensitivity, collation, indexing all behave differently once this isn't a `ConcurrentDictionary` anymore).

This is on my list for when there's a real database to test it against.

## Error handling: what I did and didn't add, and why

I kept `ErrorType` at just `NotFound` and `BadRequest`, and I haven't added any concrete `AppException` subclasses to the abstract base I built in week 1.

Both were deliberate, not oversights — I don't have a case in this codebase yet that needs `Conflict` or a specific custom exception, and adding one "just in case" is exactly the kind of speculative code that Clean Code argues against: an enum member or an exception class with nothing that ever throws or returns it isn't more complete, it's dead weight I'd have to explain at the demo without a real answer for "when does this get used?"

My plan is to add each one at the moment I hit a concrete case that needs it — `Conflict` is the obvious first candidate once week 3 adds a real database and duplicate-record scenarios become possible.

### Making error handling fail loudly

What I did tighten this week is the *safety* of that decision.

`ToHttpStatusCode` used to silently fall back to `BadRequest` for any `ErrorType` it didn't recognize — which meant if I added a new `ErrorType` later and forgot to map it here, the API would lie to the client with the wrong status code instead of failing visibly.

It now throws an `ArgumentOutOfRangeException` in that case instead.

That's the difference between "add error types when I need them" being a genuinely safe strategy versus a ticking bug: the gap between adding a case and forgetting to wire it up now fails loudly at the exact place it happens, instead of shipping silently wrong.

### Protecting internal error details

I also changed what `GlobalExceptionHandler` sends back to the client for unhandled exceptions.

It used to return `exception.Message` in the response body, which is a real problem — that message can contain internal details a client has no business seeing (a null reference on a private field name, a database connection string fragment, whatever the exception happened to be about).

It now returns a fixed, generic message for anything that isn't a known `AppException`, and the real exception (with its actual message and stack trace) only ever goes to the logger.

That's the "internal details must never leak to the client" requirement from the instructions, actually enforced instead of assumed.

## How I decided what to test, and what I didn't test

I treated testing as testing the logic I actually wrote, not testing ASP.NET Core or .NET itself.

The main question I used was:

> "Does this code contain a decision or behavior that I could accidentally break when I change it?"

If yes, it should have a test; if it is framework behavior or an implementation detail I don't own, it doesn't need a unit test.

### `BookService`

I started with `BookService`, mocking `IBookRepo` and `IMapper` with Moq and using a real `NullLogger<BookService>` rather than mocking `ILogger` — I'm not asserting on log output, so mocking it would only add noise.

Every public method on `BookService` has success and relevant failure cases, including not-found and deleted-book paths.

The methods with a real business rule (`AddBookAsync` and `UpdateBookAsync` rejecting a future `DatePublished`) use `Theory`/`InlineData` cases across different day offsets.

The update tests also verify that a rejected update does not mutate the existing entity, so the tests prove the operation actually stops rather than only returning the correct error.

### Filtering

I then extended the tests beyond `BookService` because week 2 introduced new logic outside the service.

`ApplyQueryFilters` is tested directly with real `Book` entities because filtering is application logic I wrote, not something provided by LINQ that I need to test.

The tests cover:

- No filters
- Title
- Status
- Rating
- Combinations of filters
- Cases where nothing matches

### Pagination

Pagination is tested through `InMemoryBookRepo.GetAllBooksAsync`, covering:

- First and later pages
- Page sizes
- Pages beyond the available data
- Total counts
- Soft-deleted books
- Filtering combined with pagination
- Maximum page size
- Default query values
- `PagedResult<T>.TotalPages`

This verifies the important behavior that filtering happens before pagination and that `TotalCount` represents the filtered dataset rather than just the current page.

### DTO validation

I also added validation tests for the DTOs and query parameters using `Validator.TryValidateObject`.

These cover the validation rules I explicitly defined, such as:

- Required fields
- Maximum string lengths
- Rating ranges
- Enum values
- Valid/invalid query parameters

I am not testing DataAnnotations themselves or ASP.NET Core's validation pipeline; I am testing that the rules I chose for this API behave as intended.

### What the test suite covers

The resulting suite contains **85 passing tests**.

The tests are deliberately focused on:

- Business logic
- Filtering
- Pagination
- Validation

Rather than:

- Controllers
- ASP.NET Core framework behavior
- AutoMapper internals
- The underlying `ConcurrentDictionary`

Those would either be framework behavior or belong to integration testing, which is out of scope for this week.

I am also not treating 100% code coverage as the goal. A high coverage number does not automatically mean the important behavior is tested. I care more about covering meaningful decisions and failure paths so that a failing test tells me what behavior I broke.

## What was painful to change from week 1, and what that tells me

Splitting out `App_Common` was more disruptive than I expected for something that sounds small.

`BookStatus` had been living in `App_DAL.Entities.Books`, and moving it broke every `using` that touched it — DTOs, the query params, the mapper profile, the repository.

It wasn't hard, just tedious, and the tedium is the lesson: the fact that moving one enum touched five files is a sign those files were more coupled to *where* the type happened to live than to what it actually meant.

If I'd put `BookStatus` somewhere layer-neutral in week 1 instead of parking it in `App_DAL` out of convenience, this would've been a non-event in week 2.

The entity-leak issue from week 1 — `App_PL` can transitively see `App_DAL`'s public types because `App_BLL` references `App_DAL` — is still open.

I said in week 1 I'd fix it with `internal` entities and `InternalsVisibleTo`, and I didn't get to it this week; pagination, filtering, and tests took priority.

Nothing in the controller uses `Book` today, so it's not causing a bug, but it's still a gap between what the project structure implies ("the controller can't see the entity") and what it actually guarantees ("the controller currently doesn't, but nothing stops it").

This is the first thing on my list for week 3, before the database work, because adding EF Core on top of an entity that's still technically visible from the presentation layer just gives the leak a more dangerous thing to leak.

## What I'd improve with more time

- The current unit test suite covers the main business logic, filtering, pagination, and validation rules. If the project grows, I would continue adding tests around new business decisions rather than chasing a specific coverage percentage.

- The entity-leak fix from week 1 is still outstanding and I want it done before week 3's database work, not after.

- Title filtering is exact-match only. A `Contains()` version is easy to write but I want to understand what it costs against a real database before I commit to it.

- `BookEditDto` and `BookCreateDto` are still field-for-field identical. I flagged this with a `TODO` in week 1 and haven't resolved whether that's actually a problem worth fixing or just a coincidence of this domain — I want to decide that deliberately, not just collapse them because they look similar today.
# Week 3 — EF Core, Relational Modeling, & Containerization

## What changed this week

Week 3 moved the application from the week 1/2 in-memory design to a real relational persistence layer using **Entity Framework Core with Microsoft SQL Server**.

This was not treated as a rewrite of the application. The existing layered architecture and repository/service abstractions were kept in place, and the concrete in-memory repositories were replaced by EF Core implementations through dependency injection.

At the same time, the domain model was expanded from a single `Book` entity into a relational model involving **Books, Authors, Users, and Loans**, with explicit foreign keys, referential constraints, indexes, migrations, and database-level protections.

---

## 1. Entity Refactoring & Relational Domain Modeling

### `Book.Author` became a real relationship

In week 1, `Book` stored the author's name directly as text. In week 3 I changed that design so that `Book` now stores an `AuthorId` foreign key and references an `Author` entity.

The relationship is modeled as **one Author → many Books** from the database perspective. EF Core configures `Book.AuthorId` as the foreign key and uses `DeleteBehavior.Restrict`, so deleting an author cannot silently delete its books. The database migration reflects the same restriction through the generated foreign-key constraint.

I deliberately kept the relationship unidirectional from the entity-navigation perspective for now: `Book` knows its `Author`, while `Author` does not currently expose a `Books` collection. The important architectural decision was establishing the relational ownership through `AuthorId`, not duplicating author information inside every book.

### Why I chose an `Author` entity

Making authors first-class entities avoids storing duplicated author names across books and gives the system a stable identity for an author.

This also makes future author-level operations possible without redesigning the book schema again.

The refactoring had a real impact because `Book` was already used throughout the repository, service, mapping, and test layers. Rather than changing the architecture, I adapted those layers around the new domain relationship.

---

## 2. Loan Modeling & Borrowing History

I introduced `Loan` as an **associative/domain entity** between `User` and `Book`, rather than simply placing a `UserId` and an `IsBorrowed` flag on `Book`.

A loan stores:

* `BookId`
* `UserId`
* `LoanedAt`
* `DueAt`
* `ReturnedAt`

This design intentionally preserves borrowing history. A book can therefore participate in multiple loan records over its lifetime, while only one loan may be active at a time. Returning a book does not delete the loan record; it sets `ReturnedAt`, preserving the historical event.

The database relationships from `Loan` to both `Book` and `User` also use `DeleteBehavior.Restrict`, preventing deletion of a parent record from silently destroying borrowing history.

---

## 3. Soft Delete & Data Lifecycle

I continued the soft-delete decision from week 1 rather than switching to physical deletion once SQL Server was introduced.

`Book`, `Author`, and `User` contain `IsDeleted`, `DeletedAt`, and timestamp fields, allowing records to be logically removed while remaining in the database for historical purposes.

The important distinction in week 3 is that soft delete is now backed by a persistent database rather than an in-memory flag.

For authors, the application also prevents deletion while an active, non-deleted book still references the author. That rule is checked in the service through `HasActiveBookByAuthorAsync`, while the database FK restriction provides a second layer of protection against destructive cascading behavior.

---

## 4. Switching from In-Memory Storage to EF Core + SQL Server

The main persistence decision this week was replacing `InMemoryBookRepo` with an EF Core-backed `BookRepo`.

The service layer did not need to become database-aware because `BookService` still depends on `IBookRepo`. The concrete implementation is selected through dependency injection in `Program.cs`:

`IBookRepo → BookRepo`

The same pattern is used for authors, users, and loans. This validated the repository abstraction from week 2: the abstraction was useful because changing the persistence mechanism did not require rewriting the controller/service architecture.

EF Core is configured to use SQL Server through:

`AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));`

and the DAL references the EF Core and SQL Server provider packages directly.

---

## 5. Keeping EF Core Configuration Separate from the Entities

I chose to keep database-specific configuration out of the entity classes and place it into dedicated `IEntityTypeConfiguration<T>` classes.

For example, `BookConfiguration` defines:

* primary key configuration
* the `Author` relationship
* `DeleteBehavior.Restrict`
* required fields and length limits
* decimal precision for ratings
* indexes
* unique ISBN constraint

`LoanConfiguration` similarly defines its relationships, required fields, indexes, and active-loan constraint.

`AppDbContext` then uses:

`modelBuilder.ApplyConfigurationsFromAssembly(...)`

so the context automatically discovers those configuration classes.

I preferred this approach because the entities remain focused on domain state and behavior, while persistence-specific decisions such as indexes, SQL precision, and delete behavior stay in the DAL configuration layer.

---

## 6. Database-Level Concurrency Protection

The most important concurrency decision in week 3 was the handling of simultaneous attempts to loan the same book.

Application-level checking alone is not sufficient:

`HasActiveLoanAsync(bookId)`

can return `false` for two requests that execute at nearly the same time.

Therefore, I added a **unique filtered SQL Server index** on `Loan.BookId` where `ReturnedAt IS NULL`.

This means the database itself guarantees that there can be at most one active loan for a particular book. Historical returned loans are still allowed because their `ReturnedAt` value is no longer null.

The service still performs the application-level availability check because it provides a clean and fast validation path for normal requests. The database constraint is the final protection against the race condition.

If two requests pass the application check concurrently, one insert succeeds and the other receives a `DbUpdateException`, which the service converts into a domain-level `Conflict` result instead of exposing a database error directly to the client.

This gave me an important design lesson: **application validation improves user-facing behavior, but database constraints must enforce invariants that can be violated by concurrency.**

---

## 7. Transactions & Unit of Work

I intentionally did not introduce a large explicit transaction abstraction.

The current repository operations use EF Core's `SaveChangesAsync()` for each atomic persistence operation.

For example, adding a book, creating a loan, or returning a book performs its required changes and then saves them through the DbContext.

The reasoning is that most current operations are single aggregate/database writes. Adding explicit transaction scopes around every operation would add complexity without providing additional value.

The loan concurrency case is different: rather than trying to solve it with a complex transaction flow, I chose a database uniqueness constraint because the actual invariant is simpler and more reliably expressed as a constraint.

---

## 8. Query Design & Performance Decisions

The move to SQL Server made the performance considerations from week 2 concrete.

`BookRepo.GetAllBooksAsync()` now builds an `IQueryable`, applies filters before pagination, calculates the total count, and then applies `OrderBy`, `Skip`, and `Take`. It also uses `AsNoTracking()` for read-only catalog queries.

I kept eager loading of the author with:

`Include(b => b.Author)`

because the API needs author information together with book information. Since the endpoint is paginated, the amount of materialized data is bounded by the requested page size rather than loading the complete table into memory.

I also added indexes for fields that participate in common lookups/filtering, including:

* `AuthorId`
* `DatePublished`
* `Rating`
* `Isbn`

and made ISBN unique because it represents a unique book identifier in the catalog.

This is a deliberate trade-off: indexes improve read performance but introduce additional write/storage cost, so I only added them to fields with a concrete query or integrity requirement.

---

## 9. Inspecting Generated SQL

Moving from LINQ over an in-memory collection to EF Core introduced a new concern: I needed to verify what SQL EF Core was actually generating.

For that reason, `BookRepo.GetAllBooksAsync()` uses `ToQueryString()` on the final paginated query during development. This allowed me to inspect the generated SQL while debugging instead of assuming that the LINQ expression translated into the query I intended.

This fits the lesson from week 2: once persistence becomes a real database, abstraction should hide implementation details from the business layer, but the developer still needs visibility into those details while diagnosing performance or correctness problems.

---

## 10. EF Core Migrations

The schema is managed through EF Core migrations rather than manually creating tables.

The week 3 migration creates:

* `Authors`
* `Users`
* `Books`
* `Loans`

and establishes their primary keys, foreign keys, indexes, uniqueness constraints, and default values.

I reviewed the generated migration SQL rather than treating `Add-Migration` as a black box. This gave me a way to verify that important design decisions — especially `DeleteBehavior.Restrict` and the filtered unique active-loan index — were actually represented in the database schema.

In development, the application also executes:

`db.Database.Migrate()`

during startup so the configured database is brought up to the latest migration automatically.

---

## 11. Containerization

Week 3 also moved the application toward reproducible deployment using Docker.

I created a **multi-stage Dockerfile**:

### Build stage

Uses the .NET SDK image to restore dependencies and publish the application.

### Runtime stage

Uses the lighter ASP.NET runtime image and copies only the published output into the final container.

The final container exposes port `8080` and runs `App_PL.dll`.

I chose the multi-stage approach so the final image does not need the full SDK and build-time tooling.

---

## 12. Docker Compose & Service Separation

The application and database are orchestrated as separate containers through `compose.yaml`.

There are currently two services:

* `api` — the ASP.NET Core Web API
* `db` — Microsoft SQL Server 2022

The API connects to SQL Server through the Docker service name `db`, rather than assuming the database is running on the host machine.

The SQL Server container also uses a named Docker volume:

`sql_data`

so database files survive container recreation instead of existing only inside the container filesystem.

The API is configured to wait for the database health check before starting, reducing startup failures caused by the API connecting before SQL Server is ready.

---

## 13. Configuration & Secrets

I deliberately kept database credentials outside the source code.

`compose.yaml` reads the SQL Server password and database name from environment variables, and the API receives its connection string through the environment-based configuration key:

`ConnectionStrings__DefaultConnection`

An `example.env` file is committed as a template rather than storing the actual secret values in the repository.

This keeps environment-specific configuration separate from application code and makes the same container configuration easier to reuse across development and deployment environments.

---

## 14. Database Choice

I selected **Microsoft SQL Server** instead of continuing with an embedded or in-memory provider because the application is already a .NET system and the relational requirements now justify a full SQL database.

SQL Server also fits naturally with EF Core, provides mature indexing and constraint support, and gives me a realistic production-oriented relational environment rather than a simplified test-only storage model.

The repository explicitly uses the SQL Server EF Core provider, so the application's persistence behavior now reflects the database technology it is intended to run against.

---

## 15. Testing & Reliability

Week 3 expanded the testing concern from purely business logic to persistence-related behavior and domain rules.

The important principle remained the same as week 2: I am testing the decisions and behavior owned by my application rather than trying to test EF Core itself.

The new persistence design introduced additional cases worth validating, especially:

* relational author/book behavior
* loan creation and return behavior
* duplicate active-loan attempts
* conflict handling when the database rejects a concurrent loan
* soft-deleted records
* repository queries with filters and pagination

The code specifically handles `DbUpdateException` in the loan workflow and translates a database-level uniqueness conflict into `ErrorType.Conflict`, keeping infrastructure details from leaking through the API contract.

---

## What I learned / what I would improve

The biggest architectural lesson this week was that moving from an in-memory repository to a real database changes the meaning of several earlier decisions.

Pagination, filtering, concurrency, deletion, and query composition are no longer purely in-memory concerns. They now interact with indexes, SQL translation, constraints, transactions, and database performance.

One decision I would revisit later is the amount of eager loading performed by read-heavy endpoints. `Include()` is currently appropriate because the API needs related data, but as the dataset and traffic grow, I would benchmark the generated queries and consider projection into DTO-shaped SQL queries rather than loading full entity graphs.

I would also revisit the current startup migration behavior for production deployments. Automatically applying migrations is convenient for development and containerized environments, but a production deployment strategy may eventually require migrations to be executed as an explicit deployment step instead.

Most importantly, week 3 validated that the abstractions from the previous weeks were not unnecessary structure: the repository and service contracts allowed the persistence technology to change while the business layer remained largely stable.
---