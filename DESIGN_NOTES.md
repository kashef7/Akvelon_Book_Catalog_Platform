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
