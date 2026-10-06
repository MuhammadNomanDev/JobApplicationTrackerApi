# Learning note: P1/M1a — .NET 10 retarget and build foundations

## 1. What was built and why
M1a moves the whole solution from .NET 8 to .NET 10 (which must happen before .NET 8 reaches end of support on 10 November 2026) and installs the build discipline every later milestone relies on. A `global.json` pins the SDK to the 10.0 feature band so every machine and CI uses the same toolchain. `Directory.Build.props` applies `net10.0`, nullable reference types, `TreatWarningsAsErrors`, `AnalysisLevel: latest` and `EnforceCodeStyleInBuild` to all six projects at once. `Directory.Packages.props` introduces central package management: one file holds every package version, projects reference packages without versions, and Dependabot's future PRs touch one place. An `.editorconfig` records the code-style rules. The namespace split is fixed — source code used `JobApplicationTracker.*` while projects and tests used `JobApplicationTrackerAPI.*`; everything is now `JobApplicationTrackerAPI.*` to match the repo and assembly names. The Dockerfile moves from `dotnet:8.0` to `dotnet:10.0` images so the container build keeps working after merge. Packages were bumped to their .NET 10-compatible latest stable versions (EF Core 10.0.12, FluentValidation 12.1.1, StackExchange.Redis 3.3.1, Serilog 10, Azure SDKs), with two deliberate exceptions documented below.

## 2. Code walkthrough
- **`global.json`** — pins SDK `10.0.100` with `rollForward: latestFeature`, so 10.0.401+ is accepted but 11.0 is not. Reproducible builds without freezing on a patch version.
- **`Directory.Build.props`** — imported automatically by every project. `TreatWarningsAsErrors` turned 12 ignored warnings into build errors that had to be fixed (see §6); `EnforceCodeStyleInBuild` runs the IDE analysers during build.
- **`Directory.Packages.props`** — every `PackageVersion` in one alphabetised list, versions re-verified on nuget.org on 2026-10-06. `CentralPackageTransitivePinningEnabled` also pins transitive versions.
- **The six `.csproj` files** — rewritten to the minimal form: no `Version` attributes (CPM), no repeated `TargetFramework`/`Nullable`/`ImplicitUsings` (inherited), forward-slash project references (work on every OS).
- **Namespace normalisation** — mechanical `JobApplicationTracker.` → `JobApplicationTrackerAPI.` across 96 `.cs` files, including the EF Core migration snapshot strings (which must match the renamed entity types) and fully-qualified references in code.
- **`Dockerfile`** — `aspnet:8.0`/`sdk:8.0` → `aspnet:10.0`/`sdk:10.0`. Without this, the GHCR image build on `main` would fail the moment this PR merges.
- **`RedisCacheService.cs`** — adapted to StackExchange.Redis v3 (see §6).
- **Domain entities** (`User`, `JobApplication`, `Note`, `Document`) — `= null!;` initialisers on properties set by the public constructor (see §6).

## 3. Ten likely interview questions
1. **Why pin the SDK with `global.json` instead of just installing .NET 10?** Different machines (and CI) can have several SDKs; without a pin, the newest installed SDK builds the repo, so "it builds on my machine" stops being reproducible. `latestFeature` roll-forward accepts patch updates but never silently jumps a major version.
2. **What does central package management buy you over per-project versions?** One place to see and bump every version; no diamond-dependency drift where two projects reference different versions of the same package; Dependabot PRs become one-file diffs; `CentralPackageTransitivePinningEnabled` stops transitive versions floating unexpectedly.
3. **Why `TreatWarningsAsErrors` — isn't that annoying?** Warnings are deferred bugs. The 12 warnings this repo carried included real nullability holes in the domain entities. Making them errors forces the fix at the moment it's cheapest — while you're already touching the code.
4. **What is `net10.0` vs `net8.0` in the project file, and what breaks when you change it?** It's the target framework moniker: which .NET runtime and reference assemblies compile against. Changing it can break on package incompatibility (a package with no `net10.0`/`netstandard2.0` asset), new compiler warnings (nullable analysis got stricter), and API removals.
5. **Why normalise the namespaces instead of leaving both?** Split namespaces confuse consumers (`using` the wrong root fails), break the "namespace matches assembly name" convention tools assume, and look unfinished in a portfolio review. It was mechanical and safe because nothing outside the repo references these namespaces.
6. **StackExchange.Redis v3 changed `StringSetAsync`'s expiry parameter — what happened?** v3 replaced the `TimeSpan?` expiry with an `Expiration` struct (implicit conversion from `TimeSpan`, `Expiration.Default` for no expiry). Passing `TimeSpan?` no longer compiles. The fix converts explicitly — same behaviour, new type.
7. **Why did `JsonSerializer.Deserialize<T>(value!)` become ambiguous?** In v3, `StringGetAsync` returns `RedisValue`, which implicitly converts to both `string` and `ReadOnlySpan<byte>` — and `Deserialize` has overloads for both, so the compiler can't choose. The explicit `(string?)` cast picks the overload and makes the nullability visible.
8. **Why keep Swashbuckle at 6.4.0 instead of bumping to 10.x?** Swashbuckle is deleted in M1c (replaced by the built-in OpenAPI + Scalar). Bumping it now would force a migration to the `Microsoft.OpenApi` v2 reference model for code with a two-PR lifespan. Pinning it is the honest, low-churn call, documented in `Directory.Packages.props`.
9. **Why remove `Microsoft.AspNetCore.OpenApi` entirely?** It was referenced but never used in code — dead weight. Worse, at 10.0.12 it pulls `Microsoft.OpenApi` v2, which conflicts with Swashbuckle 6.4's v1 (`Models` namespace) at compile time. M1c re-adds it when it's actually wired up.
10. **The Dockerfile pins `10.0` — why not `latest`?** `latest` moves under you: a rebuild six months later could silently change runtimes. Pinned major versions make image builds reproducible; Dependabot can propose the bump as a PR.

## 4. Hands-on exercise (20 minutes, no AI)
- [ ] Open `Directory.Packages.props` and pick one package. On nuget.org, check its dependencies for `net10.0`/`netstandard2.0` targets and confirm why it restores cleanly on the new TFM. Write one sentence.
- [ ] In `RedisCacheService.cs`, revert the `Expiration` line to pass `expiration` (`TimeSpan?`) directly and rebuild. Read the CS1503 error, then restore the fix and explain in your own words why the struct replaces the nullable.
- [ ] Run `dotnet build -c Release` and confirm **0 warnings, 0 errors**. Then temporarily set `TreatWarningsAsErrors` to `false` in `Directory.Build.props`, rebuild, and count how many warnings the repo actually carries. Set it back to `true`.
- [ ] From the `Dockerfile`, explain what each of the four stages (`base`, `build`, `publish`, `final`) does and why the SDK image isn't in the final stage.

## 5. Further reading
- .NET 10 release notes / breaking changes: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10
- Central package management: https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management
- `global.json` and roll-forward: https://learn.microsoft.com/en-us/dotnet/core/tools/global-json
- TreatWarningsAsErrors: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/errors-warnings
- StackExchange.Redis v3 release notes: https://github.com/StackExchange/StackExchange.Redis/blob/main/docs/ReleaseNotes.md

## 6. Postscript: what the upgrade actually broke (and the decisions)
The retarget surfaced everything the old build was silently tolerating:
1. **11 nullable warnings became errors (CS8618).** The domain entities' private EF Core constructors don't set non-nullable properties. Fix: `= null!;` initialisers — the codebase's existing pattern (e.g. navigation properties), honest about "EF Core sets this via reflection, the compiler can't see it". The `required` modifier was rejected: it would force every construction site to change for no runtime benefit.
2. **StackExchange.Redis 2.7 → 3.3 breaking changes** (detailed in Q6/Q7 above). Both fixes are behaviour-preserving; verified by the unchanged test suite.
3. **FluentValidation 11.9 → 12.1.1** bumped cleanly — validators use only stable `AbstractValidator`/`RuleFor` APIs. (The bigger licensing question — FluentAssertions vs AwesomeAssertions — is M1b's ADR, not this PR.)
4. **Swashbuckle pinned at 6.4.0** (see Q8) and **unused `Microsoft.AspNetCore.OpenApi` removed** (see Q9).

Measured before/after (clean `Release` builds, same machine):
- Build (.NET 8 SDK 8.0.425): 2m06s, 12 warnings, 0 errors → (.NET 10 SDK 10.0.401): 28s, 0 warnings, 0 errors (both cold builds: clean checkout, empty NuGet cache, same machine).
- Unit tests: 10 passed in ~3.3s → 10 passed in ~3.7s (net10.0).
- Integration tests: 5 passed + 3 skipped in ~3.8s → 5 passed + 3 skipped in ~4.4s (net10.0).
No behaviour changed — the numbers confirm the upgrade is mechanical.
