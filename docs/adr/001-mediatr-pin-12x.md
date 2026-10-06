# ADR 001: Pin MediatR at 12.x (do not follow to the commercial line)

- **Status:** Accepted
- **Date:** 2026-10-06
- **Deciders:** Muhammad Noman
- **Context:** P1/M1 (JobTrack .NET 10 upgrade)

## Context

The application uses MediatR for CQRS: `IRequest<T>` / `IRequestHandler<T>` pairs per
feature plus an `IPipelineBehavior<TRequest, TResponse>` for FluentValidation.
MediatR 12.2.0 (current) is licensed **Apache-2.0** (verified on nuget.org,
2026-10-06). Later major versions moved to a commercial license, which is
incompatible with this project's £0 budget and with a portfolio that must stay
freely reproducible.

## Options considered

1. **Pin MediatR 12.x** — stay on `12.2.0`, document the pin, decline future
   major-version Dependabot PRs per this ADR.
2. **In-house dispatcher** — replace MediatR with ~60 lines of our own
   `ISender` (handler resolution from DI + pipeline behaviors).
3. **Follow to the commercial line** — rejected outright: costs money and would
   make the repo unbuildable for anyone without a license.

## Decision

**Option 1: pin MediatR at 12.2.0.**

The version is recorded in `Directory.Packages.props` with a comment pointing
at this ADR. Rationale:

- Zero code churn: the CQRS pipeline, handlers and `ValidationBehavior` keep
  working untouched.
- 12.x is stable, feature-complete for our needs, and battle-tested — there is
  no functional reason to upgrade a mediator library.
- An in-house dispatcher is genuinely small, but it trades a solved problem
  (pipeline ordering, generic variance edge cases, diagnostics) for the romance
  of owning the code. For a modernisation portfolio, "we pinned the last free
  version deliberately" is the more professional story.

## Consequences

- Dependabot will eventually propose MediatR 13+/14+ — those PRs are declined
  by policy (reference this ADR).
- If a CVE is ever published against the 12.x line with no backported patch,
  this decision is revisited: the fallback is the in-house dispatcher
  (option 2), whose design is sketched in the learning note `docs/learning/p1m1b.md`.
- The pin is explicit in `Directory.Packages.props`, so the choice is visible
  to every future reader, not buried in a lock file.
