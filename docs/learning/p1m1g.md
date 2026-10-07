# P1/M1g — coverage: domain, validators, API models

## 1. What changed and why

First of the four coverage PRs (31.7% → 80%). The cheapest, most
embarrassing gap first: 48 new unit tests, no mocks for the domain, one mock
for file uploads.

- **17 domain entity tests** (`EntityTests`): `Document`, `Note`,
  `JobApplication` (constructor, `UpdateDetails`, `UpdateStatus` including the
  keep-existing-`AppliedDate` path), `User` (all mutators), and the `Email`
  value object (valid, empty, malformed, implicit conversion).
- **23 validator tests** (`CommandValidatorTests`): every command validator
  except the already-tested Register one — document, job-application,
  status, notes, login, refresh-token. `IFormFile` is mocked with Moq
  (length + content type); invalid enums via `(DocumentType)999`.
- **8 API model tests** (`ApiModelTests`): the `ApiResponse<T>` factories
  and `ApiPagination`'s computed properties (round-up division, empty set).
- **One production bug fix.** The null-file validator test threw
  `NullReferenceException` *inside the validator* instead of failing
  validation (see §5).

Coverage: 22.0% → **28.5%** locally; the ratchet floor moves 21 → 28
(CI will read higher once the DB tests run — raise it after the green run,
same as M1e).

## 2. Decisions

1. **Validators tested via `Validate()`, no TestHelper package.**
   FluentValidation's `TestHelper` is a separate package for a nicer
   assertion syntax; `validator.Validate(cmd)` plus AwesomeAssertions covers
   the same ground with zero new dependencies.
2. **`IFormFile` mocked, not faked.** It's an interface with two members we
   care about (`Length`, `ContentType`); a three-line Moq setup beats a
   hand-rolled fake.
3. **Fixed the validator instead of the test.** The NRE was production
   behaviour (upload with no file → 500, not 400). `Cascade(CascadeMode.Stop)`
   after `NotNull` — the idiomatic FluentValidation fix — rather than
   null-guards inside each `Must` lambda.
4. **Floor at 28, honestly.** Measured locally without the DB tests; the
   follow-up bump after CI is mechanical.

## 3. Verification (same machine, Release)

- Build: 0 warnings, 0 errors.
- Unit: **71/71** (23 existing + 48 new).
- Integration: 9/9 (DB tests excluded locally — no Docker; they run in CI).
- Coverage: **28.5%** (1414/4968 lines), both reports merged; gate passes
  against the new floor of 28.
- Format: `dotnet format whitespace --verify-no-changes` exits 0 (SDK
  built-in — see the M1f correction, not the old global tool).

## 4. Interview Q&A

**Q1. Why bother testing domain entities? They're trivial.**
Two reasons: they're the cheapest coverage in the codebase (pure logic, no
mocks), and the tests document intended behaviour — e.g. that re-applying
for a job keeps the *original* applied date. A future change that breaks
that invariant now breaks a test, not a user.

**Q2. What is FluentValidation's default cascade mode, and why did it bite?**
`CascadeMode.Continue`: every validator in a `RuleFor` chain runs even after
an earlier one fails. So with a null file, `NotNull` failed *and*
`Must(f => f.Length ...)` still executed → `NullReferenceException` inside
the validator, escaping as a 500 instead of a 400.

**Q3. Why `CascadeMode.Stop` rather than null-checks in the lambdas?**
It's the framework's intended mechanism: "once this rule fails, skip the
rest of the chain." Null-guards in every lambda would duplicate the
`NotNull` rule's job and rot the next time someone adds a rule.

**Q4. Why mock `IFormFile` instead of constructing a real one?**
`IFormFile` is an interface; there's no lightweight concrete implementation
worth reaching for. Three lines of Moq (`Length`, `ContentType`) give full
control, including the 11MB oversize case no real file upload could
conveniently produce in a unit test.

**Q5. Why not use FluentValidation.TestHelper?**
It buys slightly nicer assertion syntax (`ShouldHaveValidationErrorFor`) at
the cost of another test dependency. `Validate()` returns a result object;
AwesomeAssertions does the rest. Fewer packages, same proof.

**Q6. What does testing the `Email` value object prove?**
That validation lives at the domain boundary: `Email.Create` rejects empty
and malformed input by throwing, so no `User` can ever hold a bad email.
The tests pin that contract.

**Q7. Why test `ApiPagination`'s computed properties?**
Integer-division edge cases: 25 items at 10 per page is 3 pages, not 2; 0
items is 0 pages with no next/previous. These are exactly the properties a
frontend paginates against — wrong here means a broken "next page" button.

**Q8. How did a unit test catch a production bug?**
The null-file test expected a validation failure and got an NRE from inside
the validator. Before M1g, no test had ever passed a null file — the
integration tests only sent invalid-but-present files. The new tests probe
each rule's boundary, including nulls.

**Q9. Could this NRE have been caught earlier?**
Yes — by any test that validated a null file, or by setting
`ValidatorOptions.Global.DefaultClassLevelCascadeMode`... but honestly, the
cheapest catch is what happened: a test per rule, including the sad paths.
Validation logic without sad-path tests is decoration.

**Q10. Why is the floor 28 and not the CI number?**
28% is what I measured on this machine, without the Docker tests. The CI
number will be higher (the DB tests cover repositories, configurations,
`AppDbContext`). Setting the floor to a number I can't measure would be
fabrication; the bump after the green CI run is a one-line mechanical step.

## 5. Postscript — the validator NRE, in production terms

`POST /api/documents` with no file attached: the `ValidationBehavior`
calls `CreateDocumentCommandValidator.Validate`, which threw
`NullReferenceException` instead of returning failures. The
`GlobalExceptionHandler` maps unknown exceptions to 500 — so a client
omitting the file got "Internal Server Error" instead of "File is required."
One-line fix (`CascadeMode.Stop`), caught because M1g tests every rule's
null boundary. Second production bug found by the coverage milestone.

## 6. Exercise (15–30 min, do this before merging)

1. Remove the `.Cascade(CascadeMode.Stop)` line, run the
   `CreateDocumentValidator_NullFile_Fails` test, and read the stack trace.
   Restore the line. In one paragraph: why is an NRE *inside* a validator
   worse than a missing validation rule?
2. Pick one validator rule with no dedicated test (e.g. the 2000-character
   note limit's *passing* boundary at exactly 2000 chars) and add it. Run
   the suite.
3. Run `python3 scripts/coverage-gate.py --min 0` and name the largest
   still-uncovered class in the Application project — that's a candidate
   for M1h.
