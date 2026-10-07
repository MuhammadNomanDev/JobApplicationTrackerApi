# P1/M1h — Application handler coverage: learning pack

Milestone: cover the MediatR handlers (job applications, notes, documents, refresh-token)
with unit tests. 28 new tests; suite 71 → 99 unit tests, all green.
Local coverage 28.5% → **39.4%** (+10.9pp). Ratchet 31 → 39.

## What changed

- `tests/.../Features/JobApplications/JobApplicationHandlersTests.cs` — 11 tests:
  GetAll paging/status/search, Get one + not-found, Update + cache invalidation,
  Delete + cache invalidation, UpdateStatus + domain event publishing.
- `tests/.../Features/Notes/NoteHandlersTests.cs` — 7 tests: create/update/delete/get,
  happy path + not-found for each.
- `tests/.../Features/Documents/DocumentHandlersTests.cs` — 7 tests with mocked
  `IBlobStorageService` and `IFormFile`: upload naming, blob delete on document
  delete, SAS URL on get, not-found paths.
- `tests/.../Features/Auth/RefreshTokenCommandHandlerTests.cs` — 3 tests:
  rotation happy path, unknown token, expired token (login/register already covered).
- `tests/.../Helpers/MockDbSetHelper.cs` — stub `DbSet<T>.AsQueryable()` to return
  the mock itself (see Q3).

## Q&A

### 1. Why test handlers with mocked DbSets instead of a real database?
Speed and determinism. The 28 handler tests run in ~1s with zero infrastructure.
The Testcontainers SQL Server suite (M1e) already covers the real-database paths;
these unit tests pin each handler's logic — filtering, mapping, not-found errors,
cache invalidation, event publishing — without Docker.

### 2. Why can't Moq mock `FirstOrDefaultAsync` / `CountAsync` directly?
They are static extension methods on `EntityFrameworkQueryableExtensions`, not
virtual members of `DbSet<T>`. Moq can only intercept virtual/instance members.
`MockDbSetHelper` instead mocks the `DbSet<T>` with an async-capable query
provider (`TestAsyncQueryProvider`) so the *real* extension methods execute
against in-memory data.

### 3. `dbSet.AsQueryable()` didn't call `System.Linq.Queryable.AsQueryable`. Why?
`DbSet<TEntity>` declares its own `public virtual IQueryable<TEntity> AsQueryable()
=> this;` instance method (EF Core source, `DbSet.cs`). C# overload resolution
prefers instance methods over extension methods, so `_context.JobApplications
.AsQueryable()` binds to EF Core's method. It exists to disambiguate `IQueryable`
extension methods, and in production it just returns the set itself. I verified
this by dumping the call-site IL: the call target was
`Microsoft.EntityFrameworkCore.DbSet`1.AsQueryable()`, and `Queryable.AsQueryable(dbSet)`
(static call) returned the same reference while `dbSet.AsQueryable()` (extension
syntax) did not — until the mock was fixed.

### 4. Why did the GetAll tests fail with "doesn't implement IAsyncQueryProvider"?
`AsQueryable()` is virtual, so Moq intercepts it — but with no setup it did not
return the mock. The handler then ran `CountAsync` against whatever came back,
whose provider wasn't async-capable. Fix: one line in `MockDbSetHelper` —
`mockSet.Setup(m => m.AsQueryable()).Returns(() => (IQueryable<T>)mockSet.Object);`
so the stub returns the mock itself and keeps the async provider.

### 5. How do you test the cache-aside handler without a real cache?
Mock `ICacheService.GetOrCreateAsync` to invoke the factory inline:
`Returns((string k, Func<CancellationToken, Task<PagedResult<JobApplicationDto>>> f,
...) => f(ct))`. The handler's real query logic (`LoadPageAsync`) then executes
against the mocked DbSet, so paging/filter tests exercise production code.

### 6. How do you verify cache invalidation?
`_mockCache.Verify(x => x.RemoveByTagAsync("jobapps", It.IsAny<CancellationToken>()),
Times.Once)` after update/delete. Tag-based invalidation (M1d) means one assertion
covers the whole `jobapps` group.

### 7. How do you test domain-event publishing without a message bus?
Mock `IMessagePublisher` and verify `PublishAsync` was called once with an event
matching the expected aggregate id and new status — and `Times.Never` on the
not-found path, proving no event leaks on failure.

### 8. How do you test the document handlers without Azure Blob Storage?
Mock `IBlobStorageService` (`UploadAsync` → URL, `GetSasUrlAsync` → SAS URL,
`DeleteAsync` verifiable) and mock `IFormFile` (`FileName`, `ContentType`,
`OpenReadStream()` → `MemoryStream`). Assertions: the uploaded blob name ends
with the original file name (uniqueness prefix), and delete removes the blob
*before* removing the row.

### 9. Why does `RefreshTokenCommandHandler` need `IPasswordHasher` in tests?
Its constructor takes `(IAppDbContext, IPasswordHasher, IJwtTokenGenerator)` —
the hasher is used by sibling auth handlers sharing the constructor shape. An
unstubbed `Mock<IPasswordHasher>` suffices since the refresh path never calls it.
(First attempt failed to compile: `CS7036` — always check the real constructor.)

### 10. What's the coverage strategy from here?
M1h took Application-layer handlers from ~18% toward ~45%. Remaining: M1i
(infrastructure — blob storage, Service Bus, JWT generator) then M1j (API
controllers, exception mappings, Program/Startup). Each PR raises the ratchet to
its measured CI value; the floor never goes down.

## Exercise (15–20 min, do before merging)

1. In `JobApplicationHandlersTests`, break the `UpdateStatus` test on purpose:
   change the verified `NewStatus` to a different `JobStatus` and run it. Confirm
   it fails, then revert.
2. Add one test of your own: `GetAll` with a `SearchTerm` that matches
   `PositionTitle` but not `CompanyName` (e.g. seed company "Acme", title
   "Plumber", search "plumb"). Assert only that row returns.
3. Explain out loud: why does `mockSet.Setup(m => m.AsQueryable())` work —
   what C# rule makes `dbSet.AsQueryable()` hit EF Core's method instead of
   LINQ's extension?
