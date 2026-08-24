# Design Notes

This is a living document. It gets extended every week as the project grows.

## Week 1

### What I built

A REST API for a book catalog with full CRUD (create, read all, read by id, update, delete),
plus separate endpoints to update a book's status and rating. Data is validated on the way in,
Swagger is available to explore the API, key operations and errors are logged, and every
endpoint returns a consistent response shape whether it succeeds or fails.

### How it's structured

The solution is split into three projects:

- **App_DAL** — the `Book` entity and an in-memory repository (`InMemoryBookRepo`) behind an
  `IBookRepo` interface.
- **App_BLL** — DTOs, AutoMapper profiles, and `BookService`, which holds all the business
  rules and logging.
- **App_PL** — the ASP.NET Core Web API project: controllers, Swagger, and the global
  exception handler.

Dependencies only flow one way: `App_PL` → `App_BLL` → `App_DAL`. The controller never talks
to the repository directly, and it never touches the `Book` entity — it only knows about DTOs.

### Decisions I made, and why

- **`Result<T>` instead of exceptions for expected outcomes.** Things like "book not found" or
  "date published is in the future" aren't bugs, they're normal outcomes a client can trigger
  on purpose. Throwing an exception for that felt like the wrong tool, so the service returns a
  `Result` object (success/failure + message + status code) instead, and the controller just
  maps it to the right HTTP response. Exceptions are reserved for things that are genuinely
  unexpected (a real bug, a dependency failing) and those get caught once, centrally, by the
  `GlobalExceptionHandler`.
- **Three separate class-library projects instead of folders in one project.** This was a
  deliberate choice to make the layering hard to accidentally break. A folder boundary is easy
  to ignore; a project boundary shows up as a compile error if I try to jump layers I shouldn't
  jump into. (Turns out this isn't airtight — see "what I found hard" below.)
- **In-memory storage with `ConcurrentDictionary` instead of a real database.** A real database
  is out of scope until week 3, but I still wanted the storage to behave correctly under
  concurrent requests, so I used `ConcurrentDictionary<Guid, Book>` instead of a plain
  `List<Book>`/`Dictionary`, which isn't safe to write to from multiple requests at once.
- **Soft delete instead of removing records.** `DeleteBook()` just flips an `IsDeleted` flag.
  Keeps history around, and it's the same pattern a real database version of this would
  probably use (a `DeletedAt` column instead of an actual `DELETE`).
- **DTOs for every direction, mapped with AutoMapper.** The controller never accepts or returns
  the `Book` entity directly — `BookCreateDto`/`BookEditDto` define exactly what a client is
  allowed to send in, and `BookGetDto` defines exactly what they're allowed to see back. This
  stops accidental overposting (a client setting `IsDeleted` on create, for example) and means
  the entity's internal shape can change without breaking the API contract. AutoMapper handles
  the entity ↔ DTO conversion so I'm not hand-writing the same mapping code five times.
- **Interfaces for the repository and the service (`IBookRepo`, `IBookService`).** Both exist
  so the layer above never depends on a concrete class, only a contract. Concretely: the
  controller is constructed against `IBookService`, not `BookService`, and `BookService` is
  constructed against `IBookRepo`, not `InMemoryBookRepo`. This is what makes swapping
  `InMemoryBookRepo` for an EF Core repository in week 3 a one-line change in `Program.cs`
  instead of a rewrite of the service or controller.
- **An abstract `AppException` base class instead of one custom exception per error type.** A
  switch statement with one case per exception type doesn't scale — every new exception means
  editing the global handler. Instead, any exception that derives from `AppException` carries
  its own `StatusCode`, so `GlobalExceptionHandler` only ever needs two branches: "it's an
  `AppException`, use its status code" or "it's not, default to 500." Adding a new known
  exception later costs nothing at the handler level.
- **Keeping the `Book` fields minimal for now.** `Title`, `Description`, `AuthorName`,
  `DatePublished`, `Rating`, `Status`. I kept `AuthorName` as a plain string instead of a
  separate `Author` entity on purpose — modeling authors as their own entity (with a
  one-to-many or many-to-many relationship) is a real design question, but not one week 1 asked
  for, so I deferred it rather than guessing at a relationship I'd probably have to redo anyway.
- **`BookStatus` as `NotStarted` / `Started` / `Finished`.** This makes the catalog track a
  personal reading progress, not just "does this book exist" — closer to a reading list than a
  library inventory. I picked this because it gives the rating field somewhere meaningful to
  live (rating a book you haven't started doesn't make sense), and because a small, closed enum
  is easy to extend later without breaking existing data, unlike a free-text status field.

### What I'd improve with more time

- Right now the `Result` object carries an HTTP status code, which means a business-logic
  class is holding an HTTP concept. That's a layering leak the same way the entity issue below
  is — I'd move status-code mapping fully into the controller.
- Soft-deleted books stay in memory forever with no cleanup. Given more time I'd add an
  expiry + background cleanup job, or at least a manual purge endpoint.
- `GetAllBooksAsync` returns every book, every time — no paging yet. That's a week 2 task, but
  it's the most obvious thing that won't survive real data volume.

### What I found hard

The most useful thing I learned this week wasn't a topic on the list — it was discovering that
my layering wasn't as strict as I thought. `App_PL`'s `.csproj` only references `App_BLL`, not
`App_DAL`. I assumed that meant the controller physically couldn't see the `Book` entity. It
turns out project references in .NET are transitive at compile time: since `App_BLL` references
`App_DAL`, `App_PL` can see `App_DAL`'s public types too, even without referencing that project
directly. Nothing in my controller uses `Book` right now, but the compiler wouldn't stop future-me
from doing it by accident. The real fix (making DAL entities `internal` and exposing them to
`App_BLL` only via `InternalsVisibleTo`) is a week 2 task, but the lesson — that a project
reference in the `.csproj` isn't the same guarantee as "this layer literally cannot see that
layer" — is one I want to remember for every project after this one.

### Getting ahead of myself, and the plan for week 2

When week 2's instructions arrived, I realized I'd already built a chunk of what they were
asking for — the layering, the DTOs, the repository abstraction, the centralized error
handling — without having studied the topics behind them properly first. I built by instinct
and by copying patterns I'd seen, not from a solid understanding of *why* each one is the right
call. The entity-leak issue above is the clearest evidence of that: I put the pieces in place
that were supposed to prevent it (separate projects, DTOs, an `IBookRepo` abstraction) but
didn't actually understand project-reference transitivity well enough to know I hadn't closed
the gap.

So instead of moving straight to week 2's new features, my plan is to slow down first: study
SOLID, the Repository pattern, and DTO mapping properly, then go back through what I already
built and check each piece against that understanding — starting with actually fixing the
entity leak — before adding anything new like pagination, filtering, or tests. I'd rather have
a smaller set of things I can fully explain than a larger set I built on instinct.

## Week 2

### What I built

Pagination and filtering on `GET /api/books`, a `Common` project to hold shared types that
both `App_BLL` and `App_DAL` need, a full unit test suite for `BookService`, and a pass over
error handling to make it fail loudly instead of silently when something's unmapped.

### How I split the layers, and why exactly this way

Week 1 already had three layers, but by week 2 I ran into a real problem: `BookStatus` and
`BookQuery` needed to be visible to *both* `App_BLL` and `App_DAL`, and neither layer is
allowed to depend on the other in that direction (`App_DAL` can't reference `App_BLL` — that
would point the dependency arrow backwards). Putting `BookStatus` in `App_DAL` and having
`App_BLL` reference it back would've worked mechanically, but it's the wrong relationship
conceptually: `BookStatus` isn't a data-access concept, it's a domain concept both layers need
to talk about.

The fix was a fourth project, `App_Common`, that both `App_BLL` and `App_DAL` reference
directly, and that references neither of them back. It only holds things with no business logic
and no storage logic attached — `BookStatus` and `BookQuery` right now. This is Dependency
Inversion in the literal sense: instead of one layer depending on a concrete type owned by
another layer, both layers depend on a shared abstraction that belongs to neither. It also
cleaned up a smaller smell from week 1 — DTOs no longer need to reference `App_DAL` at all just
to use the `BookStatus` enum, which is one less accidental path for the entity-leak problem
below to get worse.

### What my data access abstraction hides, concretely

`IBookRepo` hides two things from `BookService`: *where* the data lives, and *how* it's
queried. `BookService` never sees `ConcurrentDictionary`, never sees LINQ against the
underlying store, and never sees anything database-specific. It calls
`GetAllBooksAsync(BookQuery)` and gets back `(IReadOnlyList<Book>, int totalCount)` — no
information about *how* that list or that count was produced. When week 3 swaps
`InMemoryBookRepo` for an EF Core-backed one, `BookService` doesn't change, because it was
never written against anything more specific than the interface.

The filtering itself lives in `App_DAL` (`ApplyQueryFilters` in `BookFilters.cs`), not in the
service, for the same reason — "how do I filter this data" is a data-access question, not a
business-logic question. `BookService` only decides *what* to filter by (it passes the mapped
`BookQuery` straight through); it doesn't know or care whether that's a LINQ `.Where()` over an
in-memory collection or eventually a SQL `WHERE` clause.

### Pagination, and why I capped it instead of trusting the client

`BookQueryParams.PageSize` clamps to a `MaxPageSize` of 50 in the property setter itself,
not in the controller or the service. If a client asks for a page size of 10,000, they silently
get 50 back instead of an error. I went back and forth on this — a `BadRequest` felt more
"correct" in a validation sense — but a client asking for too much data isn't really an invalid
request the way a missing title is; it's a request the server is allowed to satisfy partially.
Clamping keeps the endpoint usable instead of forcing every client to know and respect a
magic number, and it protects the server from a client (accidentally or not) asking for
everything at once.

`PagedResult<T>` carries `TotalCount` alongside the page of items for the same reason week 1's
self-check questions push on: a client that only sees 10 items has no way to know if there are
11 more or 11,000 more, and can't build "page 3 of 9" UI without that number. The cost of
exposing it is just running `.Count()` on the filtered query before paging it — cheap against
an in-memory collection, and something I'll need to specifically check the cost of once this is
real SQL in week 3.

### Filtering, honestly

Filtering supports `Title`, `Status`, and `Rating` right now, and `Title` is an exact match, not
a partial/contains search. I know that's a weak search experience — a client can't find "Dune"
by typing "dun" — but I'd rather ship an honest exact-match filter I understand the cost of than
a `Contains()` call I haven't thought through against a real database yet (case sensitivity,
collation, indexing all behave differently once this isn't a `ConcurrentDictionary` anymore).
This is on my list for when there's a real database to test it against.

### Error handling: what I did and didn't add, and why

I kept `ErrorType` at just `NotFound` and `BadRequest`, and I haven't added any concrete
`AppException` subclasses to the abstract base I built in week 1. Both were deliberate, not
oversights — I don't have a case in this codebase yet that needs `Conflict` or a specific
custom exception, and adding one "just in case" is exactly the kind of speculative code that
Clean Code argues against: an enum member or an exception class with nothing that ever throws
or returns it isn't more complete, it's dead weight I'd have to explain at the demo without a
real answer for "when does this get used?" My plan is to add each one at the moment I hit a
concrete case that needs it — `Conflict` is the obvious first candidate once week 3 adds a real
database and duplicate-record scenarios become possible.

What I did tighten this week is the *safety* of that decision. `ToHttpStatusCode` used to
silently fall back to `BadRequest` for any `ErrorType` it didn't recognize — which meant if I
added a new `ErrorType` later and forgot to map it here, the API would lie to the client with
the wrong status code instead of failing visibly. It now throws an `ArgumentOutOfRangeException`
in that case instead. That's the difference between "add error types when I need them" being a
genuinely safe strategy versus a ticking bug: the gap between adding a case and forgetting to
wire it up now fails loudly at the exact place it happens, instead of shipping silently wrong.

I also changed what `GlobalExceptionHandler` sends back to the client for unhandled exceptions.
It used to return `exception.Message` in the response body, which is a real problem — that
message can contain internal details a client has no business seeing (a null reference on a
private field name, a database connection string fragment, whatever the exception happened to
be about). It now returns a fixed, generic message for anything that isn't a known
`AppException`, and the real exception (with its actual message and stack trace) only ever goes
to the logger. That's the "internal details must never leak to the client" requirement from the
instructions, actually enforced instead of assumed.

### How I decided what to test, and what I didn't test

I put the unit tests entirely on `BookService`, mocking `IBookRepo` and `IMapper` with Moq, and
using a real `NullLogger<BookService>` rather than mocking `ILogger` — I'm not asserting on log
output anywhere, so a mock would've just added noise without checking anything. Every public
method on `BookService` has at least a success case and a not-found case, and the two methods
with a real business rule (`AddBookAsync` and `UpdateBookAsync` rejecting a future
`DatePublished`) get `Theory`/`InlineData` cases across a few different day offsets instead of
one hardcoded value, plus an explicit assertion that the entity was *not* mutated when the
update is rejected — I wanted the test to prove the rejection actually short-circuits, not just
that the return value looks like a failure.

I chose the service layer first because that's where the actual decisions live — "is this
book deleted," "is this date in the future," "what does a not-found look like." Testing the
controller would mostly be testing that ASP.NET Core correctly wires up model binding, which
isn't my code. What I haven't tested yet is `ApplyQueryFilters` in `App_DAL` directly — it's a
pure static function over `IQueryable<Book>` with no dependencies to mock, which makes it the
cheapest test I haven't written, not the hardest. That's next, before I add anything new.

### What was painful to change from week 1, and what that tells me

Splitting out `App_Common` was more disruptive than I expected for something that sounds small.
`BookStatus` had been living in `App_DAL.Entities.Books`, and moving it broke every `using`
that touched it — DTOs, the query params, the mapper profile, the repository. It wasn't hard,
just tedious, and the tedium is the lesson: the fact that moving one enum touched five files is
a sign those files were more coupled to *where* the type happened to live than to what it
actually meant. If I'd put `BookStatus` somewhere layer-neutral in week 1 instead of parking it
in `App_DAL` out of convenience, this would've been a non-event in week 2.

The entity-leak issue from week 1 — `App_PL` can transitively see `App_DAL`'s public types
because `App_BLL` references `App_DAL` — is still open. I said in week 1 I'd fix it with
`internal` entities and `InternalsVisibleTo`, and I didn't get to it this week; pagination,
filtering, and tests took priority. Nothing in the controller uses `Book` today, so it's not
causing a bug, but it's still a gap between what the project structure implies ("the
controller can't see the entity") and what it actually guarantees ("the controller currently
doesn't, but nothing stops it"). This is the first thing on my list for week 3, before the
database work, because adding EF Core on top of an entity that's still technically visible from
the presentation layer just gives the leak a more dangerous thing to leak.

### What I'd improve with more time

- `ApplyQueryFilters` needs its own unit tests — it's untested right now, and it's the cheapest
  test in the codebase to write.
- The entity-leak fix from week 1 is still outstanding and I want it done before week 3's
  database work, not after.
- Title filtering is exact-match only. A `Contains()` version is easy to write but I want to
  understand what it costs against a real database before I commit to it.
- `BookEditDto` and `BookCreateDto` are still field-for-field identical. I flagged this with a
  `TODO` in week 1 and haven't resolved whether that's actually a problem worth fixing or just
  a coincidence of this domain — I want to decide that deliberately, not just collapse them
  because they look similar today.