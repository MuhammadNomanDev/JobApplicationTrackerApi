# ADR 002: Migrate test assertions from FluentAssertions to AwesomeAssertions

- **Status:** Accepted
- **Date:** 2026-10-06
- **Deciders:** Muhammad Noman
- **Context:** P1/M1 (JobTrack .NET 10 upgrade)

## Context

The test projects assert with FluentAssertions 6.12.0. That version is
Apache-2.0, but the FluentAssertions project moved to a commercial license from
v8 onwards. Staying on 6.12.0 means freezing the assertion library forever:
no new features, and any future CVE would force a rushed migration.
AwesomeAssertions is the community continuation of the FluentAssertions API,
licensed **Apache-2.0** (verified on nuget.org, 2026-10-06, latest 9.6.0),
actively maintained, and intentionally API-compatible.

## Options considered

1. **AwesomeAssertions 9.6.0** — near drop-in replacement: same `Should()`
   syntax, so the migration is `using` directives plus any small API deltas.
2. **Shouldly** — a different assertion style (`x.ShouldBe(y)`); every test
   would be rewritten for no functional gain.
3. **Stay on FluentAssertions 6.12.0** — rejected: pins the test stack to a
   dead version line with no upgrade path.

## Decision

**Option 1: migrate to AwesomeAssertions 9.6.0.**

The test surface here is small and standard (`.Should().Be()`,
`.NotBeNull()`, `.NotBeEmpty()`, `.ThrowAsync<T>().WithMessage()`), all of
which exist identically in AwesomeAssertions, so the migration is mechanical
and the suite itself proves nothing changed.

## Consequences

- `FluentAssertions` is removed from `Directory.Packages.props`; tests use
  `AwesomeAssertions` 9.6.0.
- Assertion style stays `Should()`-based — contributors familiar with
  FluentAssertions need no retraining.
- If AwesomeAssertions ever changes license, the same playbook applies: the
  assertion syntax is the asset, the package is replaceable.
