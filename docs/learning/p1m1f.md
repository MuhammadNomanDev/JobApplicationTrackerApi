# P1/M1f — .NET 8 → .NET 10 upgrade log

## 1. What changed and why

Docs-only milestone. `docs/UPGRADE-NET8-TO-NET10.md` records the whole
upgrade: why it happened (EOL 10 Nov 2026), the six-PR strategy, the measured
evidence, the decisions, the gotchas, and what's still open. This closes
P1/M1.

Also corrected in this PR: `docs/learning/p1m1e.md`'s formatting section,
which mixed up the SDK's built-in `dotnet format` with the old `dotnet-format`
global tool (see gotcha 5 in the upgrade log).

## 2. Verification

- No code changed — docs only. `git diff --stat` shows the two markdown
  files; build and tests are unaffected.
- Every number in the upgrade log traces to a measured run recorded in the
  M1a–M1e learning docs, not to memory.

## 3. Interview Q&A

**Q1. Why upgrade a working API from .NET 8 to .NET 10?**
.NET 8 leaves support on 10 Nov 2026 — no more security patches. Beyond
that, the retarget itself paid off measurably (cold build 2m06s → 28s,
warnings 12 → 0), and newer framework pieces (HybridCache, built-in OpenAPI,
SDK `dotnet format`) let us delete third-party dependencies.

**Q2. Why six small PRs instead of one big upgrade PR?**
Blast radius. The riskiest change (retarget) went first with maximum time to
react; each PR was independently reviewable, revertable, and green in CI.
A big-bang PR mixes "the SDK changed" with "the API changed" and makes every
regression a murder mystery.

**Q3. What does central package management buy you?**
One file (`Directory.Packages.props`) owns every package version — no more
version drift between projects, and Dependabot updates land in one place.
It did break the Docker restore layer (props files must be copied before
`dotnet restore`), which is gotcha 1 in the log.

**Q4. Why treat warnings as errors?**
Warnings rot. With `TreatWarningsAsErrors`, the build caught an obsolete
Testcontainers constructor and a dead package reference instead of letting
them sit. It converts "we'll fix it later" into "fix it now."

**Q5. Why pin MediatR at 12.2.0?**
It's the last Apache-2.0 release; later versions changed licence. The ADR
records the decision so Dependabot's major-bump PRs get declined
deliberately, not accidentally. Same logic for leaving FluentAssertions at
6.12.0 vs moving to AwesomeAssertions.

**Q6. Why replace Swashbuckle instead of upgrading it?**
.NET 10 ships OpenAPI generation in the framework (`Microsoft.AspNetCore.OpenApi`)
plus Scalar for the UI. Fewer third-party dependencies, and the Bearer scheme
wiring is a small `IOpenApiDocumentTransformer` instead of Swashbuckle config.

**Q7. What did the testing milestone actually prove?**
That the tests exercise the real stack. The Testcontainers journey tests
caught a genuine production bug — `Guid.Empty` user ids violating the Users
foreign key — that no mock-based test could see, because no test had ever
written to a real database.

**Q8. What's a coverage ratchet and why not just "write more tests"?**
The ratchet (`coverage-gate.py` + `tests/coverage-baseline.txt`) is a guard
rail, not a goal: it fails CI if coverage ever drops below the committed
floor (31%). "Write more tests" is the separate 80% milestone — the ratchet
makes sure that work never silently regresses.

**Q9. Why did the formatter break CI, and what was the real lesson?**
The snippet mixed two programs: the SDK's built-in `dotnet format`
(`--verify-no-changes`) and the old `dotnet-format` global tool (`--check`).
The lesson isn't about flags — it's that I verified the wrong binary
locally. Verify the exact command CI will run, not a lookalike.

**Q10. What's deliberately left undone?**
Coverage at 31% (the 80% push is its own milestone), Azure OIDC deployment
(the Deploy job skips without credentials), and the Angular frontend
retarget. An upgrade log should say what it didn't do — that's what makes
the roadmap credible.

## 4. Exercise (15–30 min, do this before merging)

1. Open `docs/UPGRADE-NET8-TO-NET10.md` §4 and pick one number. Trace it back
   to the M1a–M1e learning doc it came from. If you can't trace it, that's a
   doc bug — say so.
2. In your own words (2–3 sentences): why did the retarget go first and the
   testing milestone go last? What breaks if you reverse them?
3. Run `dotnet format whitespace --verify-no-changes` in the repo root
   (no solution argument — it finds the .sln itself) and confirm exit 0.
