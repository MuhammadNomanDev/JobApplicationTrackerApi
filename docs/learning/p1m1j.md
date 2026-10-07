# P1/M1j — API layer coverage: learning pack

Milestone: cover the API layer (controllers, exception middleware, OpenAPI
transformer) with unit tests. 29 new tests; suite 114 → 143 unit tests, all
green. Local coverage 43.0% → **46.8%** (+3.8pp). Ratchet 43 → 46.

This closes the four-PR coverage push (M1g domain → M1h handlers → M1i
infrastructure → M1j API).

## What changed

Tests (`tests/.../Api/`):
- `Controllers/JobApplicationsControllerTests` (9): status-filter parsing
  (valid/invalid), get/create/update/delete, URL-vs-body ID mismatch → 400,
  `CreatedAtAction` route values.
- `Controllers/AuthControllerTests` (3): register/login/refresh send the
  right command and return 200 with the `ApiResponse` envelope.
- `Controllers/NotesControllerTests` (5): get/create/update/delete incl.
  ID-mismatch 400.
- `Controllers/DocumentsControllerTests` (4): get/list/upload/delete;
  upload builds `CreateDocumentCommand` from the three `[FromForm]` parts.
- `Middleware/GlobalExceptionHandlerTests` (6): ValidationException → 400
  with `errors` extension; UnauthorizedAccessException → 401;
  KeyNotFoundException → 404 with the exception message; unexpected → 500
  with detail hidden in Production, shown in Development; clean pass-through.
- `OpenApi/BearerSecuritySchemeTransformerTests` (2): Bearer scheme added
  (http/bearer/JWT, header), global security requirement added, existing
  `Components` reused rather than replaced.

## Q&A

### 1. What do controller tests actually prove?
Controllers are thin adapters over MediatR. The tests pin the adapter
contract: the *exact* command/query object sent (field by field via
`It.Is<T>`), and the HTTP shape returned (`OkObjectResult`,
`CreatedAtActionResult` with action name + route values, `BadRequest`,
`NoContent`). If someone renames an action or drops a route value, a test
fails — that's the point.

### 2. Why `It.Is<GetAllJobApplicationsQuery>(q => ...)` instead of `It.IsAny`?
`It.IsAny` would pass even if the controller sent a wrong page number or
dropped the status filter. The predicate asserts the controller's *mapping*
logic — e.g. that the string `"Applied"` became `JobStatus.Applied`, and that
`"not-a-status"` became `null` rather than throwing.

### 3. How do you test middleware without a web server?
`GlobalExceptionHandler` is a plain class: construct it with a
`RequestDelegate` (a lambda that throws), a mocked logger/environment, and a
`DefaultHttpContext` whose `Response.Body` is a `MemoryStream`. Call
`InvokeAsync`, then read the body back and deserialize the problem JSON.
No TestServer needed.

### 4. Why does the 500 detail differ by environment?
`Detail = _environment.IsDevelopment() ? exception.Message : "An internal
server error occurred."` — leaking internals (e.g. "db exploded") to
clients is an information-disclosure risk; in Development the real message
speeds up debugging. Two tests pin both branches via a mocked
`IWebHostEnvironment.EnvironmentName`.

### 5. How do you check the `errors` extension survived JSON?
`ProblemDetails.Extensions["errors"]` is serialized inline. The test reads
the raw body and asserts `errors` is a 1-element JSON array — proving the
validation failures actually reach the client, not just the status code.

### 6. Why pass `null!` as the transformer context?
`BearerSecuritySchemeTransformer.TransformAsync` never reads the
`OpenApiDocumentTransformerContext` — it only mutates the document.
(`OpenApiDocumentTransformerContext` has no public constructor in this
version, so constructing one isn't an option anyway.)

### 7. What does the transformer test guard against?
A regression to the M1c Scalar setup: if the Bearer scheme or the global
security requirement goes missing, the Scalar UI loses its "Authorize"
button and every authenticated endpoint becomes untestable from the docs.

### 8. Why do update actions return 400 on ID mismatch?
`UpdateJobApplication(Guid id, [FromBody] UpdateJobApplicationCommand command)`
checks `id != command.Id` and returns `BadRequest` *without* calling the
mediator (verified `Times.Never`). Guards against a client PUTting to one
URL with another entity's body.

### 9. What's still uncovered in the API project?
`Program.cs` (188 lines of startup wiring) — exercised by the integration
suite's `WebApplicationFactory`, not unit-testable in isolation.
`NotificationConsumer` (Infrastructure) still needs a hosted-service harness.

### 10. Where did the four-PR push land?
M1g 28.5% → M1h 39.4% → M1i 43.0% → **M1j 46.8%** (local, excl. Docker DB
tests; CI runs Testcontainers too, so expect ~55%+ there). 143 unit + 9
integration tests, all green, format clean, zero warnings. The 80% target
remains: next biggest pools are `Program.cs`/hosting, repositories (covered
in CI), and deeper handler branches.

## Exercise (15–20 min, do before merging)

1. In `JobApplicationsControllerTests`, change the expected status in
   `GetAll_ParsesStatusFilter_AndReturnsOk` from `JobStatus.Applied` to
   `JobStatus.Draft` and run it. Confirm it fails, then revert.
2. Add a test: `GlobalExceptionHandler` with a `ValidationException`
   carrying *two* failures → assert `errors` has 2 entries.
3. Explain out loud: why is `Times.Never` on the mediator in the
   ID-mismatch test the most important assertion in that test?
