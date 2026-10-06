# Learning note: P1/M1b — Licensing ADRs (MediatR pin, AwesomeAssertions)

## 1. What was built and why
M1b answers the question every .NET 10 upgrade eventually hits: two of our dependencies changed their commercial terms, so we decide deliberately instead of drifting. **ADR-001** pins MediatR at 12.2.0 — the last Apache-2.0 version (verified on nuget.org 2026-10-06; v14 is the commercial line). **ADR-002** migrates the test assertions from FluentAssertions 6.12.0 to AwesomeAssertions 9.6.0 (Apache-2.0, the community continuation of the same API). Both ADRs live in the new `docs/adr/` folder: Context, Options, Decision, Consequences — the format interviewers expect. The implementation is exactly what the ADRs say: a pin comment on the MediatR version in `Directory.Packages.props`, and `FluentAssertions` → `AwesomeAssertions` in the package list, the two test project files, and five `using` directives. No test logic changed.

## 2. Code walkthrough
- **`docs/adr/001-mediatr-pin-12x.md`** — the pin decision. Records the three options (pin 12.x, in-house dispatcher, follow the commercial line — rejected), why pinning wins (zero churn, stable, free), and the policy: Dependabot majors past 12.x are declined per this ADR; a future CVE reopens the dispatcher option.
- **`docs/adr/002-awesomeassertions.md`** — the migration decision. Why not stay on 6.12.0 (dead version line, no upgrade path) or move to Shouldly (every test rewritten for no gain).
- **`Directory.Packages.props`** — `AwesomeAssertions` 9.6.0 replaces `FluentAssertions` 6.12.0; MediatR keeps `12.2.0` with ADR comments on both lines so the reasoning is visible where the version is read.
- **Five test files** — `using FluentAssertions;` → `using AwesomeAssertions;`. The assertion surface (`.Should().Be()`, `.NotBeNull()`, `.NotBeEmpty()`, `.ThrowAsync<T>().WithMessage()`) exists identically in AwesomeAssertions, so nothing else changed.
- **`docs/learning/p1m1b.md`** — this file.

## 3. Ten likely interview questions
1. **What is an ADR and why write one for a version pin?** An Architecture Decision Record captures a significant decision: context, options, the choice, and consequences. A pin looks trivial until Dependabot proposes v14 six months later and someone has to remember *why* we don't upgrade. The ADR is that memory.
2. **Why not just upgrade MediatR to v14?** v14 moved to a commercial license. This project runs on a £0 budget and must stay reproducible for anyone who clones it — a paid dependency breaks both.
3. **Why not write your own dispatcher instead of pinning an old version?** Honestly considered (it's ~60 lines). But MediatR 12.x is stable and free; replacing it trades solved edge cases (pipeline ordering, generic variance) for the romance of owning the code. Pinning is the professional call here.
4. **What's the risk of pinning, and what's the escape hatch?** Risk: a CVE in the 12.x line with no backported patch. Escape hatch, documented in the ADR: build the in-house dispatcher then — the design is sketched below, not built speculatively.
5. **Why migrate off FluentAssertions 6.12.0 if that version is still free?** The *project* went commercial at v8. Staying on 6.12 means freezing the test stack on a dead line forever — no fixes, no features, and a forced rushed migration if a CVE appears.
6. **Why AwesomeAssertions over Shouldly?** AwesomeAssertions continues the FluentAssertions API, so the migration is mechanical. Shouldly's `x.ShouldBe(y)` style would rewrite every assertion for zero functional gain.
7. **How do you know the migration didn't change test semantics?** The suite itself is the proof: identical tests, identical results (10/10 unit, 5+3 integration) before and after. The change is confined to `using` directives and the package reference.
8. **What does central package management have to do with this?** The pin and the swap each touch exactly one line in `Directory.Packages.props`. Without CPM they'd be scattered across project files — easy to miss one and end up with two assertion libraries.
9. **How would you handle a Dependabot PR for MediatR 14?** Decline it with a link to ADR-001. That's the whole point of writing the decision down: the policy outlives the person who made it.
10. **Sketch the in-house dispatcher you'd build if forced off MediatR.** `ISender` with `Send<TResponse>(IRequest<TResponse>, CancellationToken)`; resolve `IRequestHandler<TRequest, TResponse>` from DI; run `IPipelineBehavior<TRequest, TResponse>` chain around it (validation behavior already exists and ports directly). Registration: scan assemblies for handler types, `AddTransient` each closed interface.

## 4. Hands-on exercise (15 minutes, no AI)
- [ ] Open `Directory.Packages.props` and, for MediatR and AwesomeAssertions, verify the license claim yourself: open each package's nuget.org page and find the license. Write down what you found.
- [ ] In one test file, temporarily change an assertion to the Shouldly style (`result.ShouldBe(...)`) and confirm it doesn't compile — then explain why the team standard is the `Should()` style.
- [ ] Draft the decline comment you'd leave on a hypothetical Dependabot PR bumping MediatR to 14.x, referencing ADR-001.
- [ ] From memory, whiteboard the in-house dispatcher design from Q10: interfaces, DI registration, and where the validation behavior plugs in.

## 5. Further reading
- ADR basics (Michael Nygard's original format): https://github.com/joelparkerhenderson/architecture-decision-record
- MediatR licensing: https://github.com/jbogard/MediatR
- AwesomeAssertions: https://awesomeassertions.org/
- NuGet central package management: https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management

## 6. Postscript: the migration was a non-event (which is the point)
The entire FluentAssertions → AwesomeAssertions migration compiled on the first build and passed the full suite unchanged. That's not luck — it's what "API-compatible continuation" means, and it's why the ADR could recommend it confidently: the risk was assessed (small, standard assertion surface) before the change, and the suite verified it after. Boring migrations are good migrations.
