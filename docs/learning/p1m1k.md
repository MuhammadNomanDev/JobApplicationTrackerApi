# P1/M1k — prune dead code, consumer test: learning pack

Milestone: delete ~200 lines of dead repository/UnitOfWork code and cover
the notification consumer's disabled path. 1 new test; suite 143 → 144 unit
tests, all green. Local coverage 46.8% → **50.5%** (+3.7pp). Ratchet 46 → 50.

## What changed

Deleted (dead code — registered in DI but never injected anywhere; every
handler uses `IAppDbContext` directly):
- `Infrastructure/Persistence/Repositories/` — 4 repositories
  (JobApplication, Note, Document, User)
- `Infrastructure/Persistence/UnitOfWork.cs` + `Application/Interfaces/IUnitOfWork.cs`
- `Application/Interfaces/` — 4 repository interfaces
- DI registrations + orphaned usings in `DependencyInjection.cs`

Added:
- `tests/.../Services/NotificationConsumerTests.cs` — disabled-path test.

## Q&A

### 1. How did you prove the repositories were dead?
Grepped every `src` reference to the five interfaces: the only hits outside
their own definitions were the `AddScoped` lines in `DependencyInjection.cs`.
No constructor anywhere takes them — the handlers all depend on
`IAppDbContext`. Registered-but-never-resolved = dead.

### 2. Why delete instead of covering them?
Coverage measures *tested* code, not *needed* code. Testing dead code
locks in an API nobody uses and inflates the suite. Deleting shrinks the
denominator honestly — the percentage goes up because there's less
unneeded code, not because of clever accounting.

### 3. Why is the consumer tested via a subclass, not StartAsync?
`BackgroundService.StartAsync` launches `ExecuteAsync` on a background
task — the test would race it (the log assertion ran before the method
body). A test-only subclass exposes `ExecuteAsync` directly, making the
test deterministic. Framework machinery isn't what we're testing.

### 4. Why a hand-rolled RecordingLogger instead of Moq?
Verifying `ILogger.Log` with Moq needs an `It.Is<It.IsAnyType>` predicate
inside an expression tree — null-propagation is banned there and the
nullability annotations fight you. A 15-line recording fake is clearer,
has no such restrictions, and asserts on the actual message text.

### 5. What does the consumer test actually pin?
That with no `AzureServiceBus` config (dev/test), exactly one message
containing "disabled" is logged and the method returns — i.e. it never
reaches `new ServiceBusClient(...)`. If someone reorders the guard, the
test fails instead of the suite hanging on a network call.

### 6. Why wasn't Program.cs covered in this milestone?
Its remaining gaps are environment branches (`IsDevelopment`,
connection-string presence) — one side is inherently untestable in a
unit run, and the integration suite already exercises the pipeline.
Contorting tests for it would cost more than it teaches.

### 7. What's the coverage story now?
46.8% → 50.5% local. About half the gain is denominator shrinkage
(deleted dead code), half is the new test. CI will read higher with the
Testcontainers DB tests. The honest 80% would need Program.cs hosting
branches — diminishing returns from here.

### 8. Could the dead code have been caught earlier?
Yes — a NetArchTest rule ("no registered service may be unreferenced")
or a DI-container self-check test would flag it. Worth adding if the
codebase grows.

### 9. Did anything else reference the deleted types?
No — full grep of `src` and `tests` after deletion showed zero hits.
Build is clean with `TreatWarningsAsErrors`.

### 10. What remains uncovered that's worth doing?
`NotificationConsumer`'s receive loop (needs a receiver seam — do it when
the consumer becomes real, not while it's a placeholder), deeper handler
branches, `Program.cs` environment branches.

## Exercise (15 min, do before merging)

1. Run `git show --stat HEAD` on the branch. Confirm the 10 deletions and
   explain why each was safe to remove.
2. In `NotificationConsumerTests`, change the assertion to
   `ContainSingle(m => m.Contains("enabled"))` and run it. Confirm it
   fails, then revert.
3. Explain out loud: why does testing `ExecuteAsync` directly give a
   stronger test than going through `StartAsync`?
