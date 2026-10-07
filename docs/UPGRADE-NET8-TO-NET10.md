# .NET 8 → .NET 10 upgrade log

How the JobApplicationTracker API moved from .NET 8 to .NET 10, one small PR
at a time — with the measurements to prove each step. Written October 2026;
.NET 8 reaches end-of-support on **10 November 2026**.

## 1. Why upgrade

- **End of support.** .NET 8 stops receiving security patches on 10 Nov 2026.
  Running a portfolio API on an unsupported runtime is the opposite of the
  story this project tells ("I modernise .NET systems").
- **Measured payoff, not fashion.** The retarget alone cut cold build time
  from 2m06s to 28s and took warnings from 12 to 0 (see §4). Newer framework
  features (HybridCache, built-in OpenAPI, `dotnet format` in the SDK)
  replaced third-party dependencies in later milestones.
- **Interview signal.** "I upgraded a real API across a major version with
  green CI at every step" beats "I know .NET 10 exists."

## 2. Strategy: six small PRs, not a big bang

One milestone = one branch = one PR; CI green before each merge. The
sequence was deliberate — foundations first, behaviour last:

| PR | Milestone | What |
|----|-----------|------|
| #12 | M1a | SDK pin, `net10.0` retarget, central package management, nullable + warnings-as-errors, `.editorconfig`, namespace normalisation |
| #13 | M1b | Licensing ADRs (MediatR pin, AwesomeAssertions) |
| #14 | Hotfix | Docker restore fix for central package management |
| #15 | M1c | API modernisation: OpenAPI + Scalar, health split, rate limiting, RFC 9457 problems |
| #16 | M1d | HybridCache replaces hand-rolled Redis service |
| #17 | M1e | Testcontainers SQL Server, architecture tests, format gate, coverage ratchet |

Each PR was independently reviewable and revertable. The riskiest change
(retarget) went first, while there was maximum time to react.

## 3. What changed, per milestone

**M1a — retarget.** `global.json` pins SDK 10.0.100; `Directory.Build.props`
sets `net10.0`, `TreatWarningsAsErrors`, `AnalysisLevel latest`;
`Directory.Packages.props` centralises every version; `.editorconfig`
standardises style; 96 files normalised to `JobApplicationTrackerAPI.*`;
entity `= null!` for 11 CS8618s; Dockerfile moved to `dotnet:10.0` images.

**M1b — licensing.** Two ADRs: MediatR pinned to 12.2.0 (last Apache-2.0;
Dependabot majors declined per ADR) and AwesomeAssertions 9.6.0 replacing
FluentAssertions (commercial from v8). No test logic changed — the suite
passing unchanged *was* the proof.

**Hotfix — Docker + CPM.** Central package management broke the Docker
restore layer (props files arrived after `dotnet restore`). Fixed by copying
`Directory.Packages.props`, `Directory.Build.props` and `global.json`
before the csproj files; verified live via the GHCR image.

**M1c — API modernisation.** Swashbuckle → `Microsoft.AspNetCore.OpenApi`
10.0.12 + Scalar 2.17.14 (JWT Bearer scheme via `IOpenApiDocumentTransformer`);
`/health` split into `/health/live` + `/health/ready` (SQL check only when a
connection string exists); fixed-window rate limiting (10/min/IP) on
`/api/auth/*`; `application/problem+json` with RFC 9457 type URIs.

**M1d — HybridCache.** Deleted the hand-rolled `RedisCacheService`;
`HybridCacheService` over `Microsoft.Extensions.Caching.Hybrid` gives
in-memory L1 always and Redis L2 when configured, stampede protection via
`GetOrCreateAsync`, and tag invalidation replacing the Redis `KEYS` scan.

**M1e — testing.** Testcontainers SQL Server (un-skipped the 3 P0-parked 404
tests, added 2 journey tests); NetArchTest layering tests; `dotnet format`
gate; Cobertura coverage on every run with a ratcheted floor. The journey
tests caught a real production bug: `CreateJobApplicationCommandHandler`
used a `Guid.Empty` placeholder for the user id, so every create violated
the `Users` foreign key — fixed by reading the id from the JWT claims.

## 4. Measured evidence

All measurements taken on the same machine (Release), before → after:

| Metric | .NET 8 (pre-M1a) | .NET 10 (post-M1e) |
|--------|------------------|---------------------|
| Cold build | 2m06s, 12 warnings | 28s, 0 warnings |
| Unit tests | 10/10 | 23/23 |
| Integration tests | 5 passed, 3 skipped | 14 passed (5 need Docker) |
| Line coverage (excl. generated) | 18.7% | 31.7%, ratcheted at 31 |
| API docs | Swashbuckle | OpenAPI + Scalar, Bearer-aware |
| Cache | Redis-or-nothing | HybridCache (L1 always, Redis L2) |
| `dotnet format --check` | n/a | clean (SDK built-in) |

## 5. Key decisions

- **Retarget first, modernise after.** The compiler and SDK upgrade was kept
  behaviour-free so any regression had exactly one suspect.
- **Central package management.** One file owns every version; Dependabot
  majors on MediatR are declined by ADR, not by accident.
- **Warnings as errors.** The build caught real issues (an obsolete
  Testcontainers constructor, the unused `Microsoft.AspNetCore.OpenApi`
  reference) instead of letting them rot as warnings.
- **Replace, don't wrap.** Swashbuckle, the Redis client and the EF
  in-memory-style skips were removed, not adapted — less code, fewer lies.
- **Tests prove the stack, not the mocks.** The 404 and journey tests run
  against real SQL Server in Docker; the first run caught a genuine FK
  bug no mock-based test could see.

## 6. Gotchas (so the next upgrade is cheaper)

1. Docker `COPY` order matters with CPM — props files must precede restore.
2. GitHub Actions rejects `secrets` in any `if:`; map to `env:` first.
3. MSBuild imports only the *nearest* `Directory.Build.props` — nested ones
   must chain to the root explicitly.
4. `coverlet.collector` ignores MSBuild coverage properties; `coverlet.msbuild`
   is what makes plain `dotnet test` collect.
5. `dotnet format` (SDK built-in, `--verify-no-changes`) and `dotnet-format`
   (old global tool, `--check`) are different CLIs — don't mix the flags.
6. Testcontainers 4.15 obsoleted the parameterless `MsSqlBuilder()`; the
   image goes in the constructor.

## 7. What's still open

- **Coverage → 80%.** The ratchet holds 31%; reaching 80% is its own
  milestone (more handler/service unit tests), deliberately scoped out of M1e.
- **Azure deployment.** The Deploy job is skipped without `AZURE_CREDENTIALS`;
  proper OIDC deployment is P1/M2 work.
- **Angular 22 frontend.** Unaffected by this upgrade; retarget planned
  separately.
