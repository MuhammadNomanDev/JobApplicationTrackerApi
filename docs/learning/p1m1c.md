# P1/M1c — API modernisation: OpenAPI + Scalar, health split, rate limiting, RFC 9457

## 1. What changed and why

Four independent improvements to the API surface, one PR:

1. **Swashbuckle → `Microsoft.AspNetCore.OpenApi` + Scalar.**
   Swashbuckle.AspNetCore 6.4.0 was the last pre-Microsoft.OpenApi-v2 dependency.
   The built-in `AddOpenApi()` generates the document at runtime from the same
   `ApiExplorer` metadata; Scalar replaces the Swagger UI. One fewer third-party
   dependency in the request path, and the document model is now the
   Microsoft-maintained one.
2. **`/health` → `/health/live` + `/health/ready`.**
   A single health endpoint conflates two different questions. Liveness ("is the
   process up?") drives container *restarts*; readiness ("can it serve traffic?")
   drives load-balancer *rotation*. A DB blip should take a pod out of rotation,
   not recycle the container.
3. **Rate limiting on `/api/auth/*`.**
   Login, register and refresh-token are the credential-stuffing targets: 10
   requests/minute per client IP, then 429 + `Retry-After`.
4. **RFC 9457-clean problem responses.**
   `GlobalExceptionHandler` now returns `application/problem+json` (it was
   `application/json`, the wrong media type) and sets a stable `type` URI per
   problem kind.

## 2. Decisions

1. **Scalar over Swagger UI / Redoc.** Scalar is the UI the ASP.NET Core team
   now ships samples with; it reads the same OpenAPI document, so the choice is
   presentation-only and trivially reversible.
2. **`Microsoft.AspNetCore.OpenApi` 10.0.12 + `Scalar.AspNetCore` 2.17.14**,
   pinned in `Directory.Packages.props` like everything else.
3. **The JWT Bearer scheme is ported, not dropped.**
   `BearerSecuritySchemeTransformer` (`IOpenApiDocumentTransformer`) re-adds the
   security definition and the document-level security requirement, so Scalar
   keeps its "Authorize" button with identical behaviour to the Swashbuckle era.
4. **Written against the Microsoft.OpenApi v2 model.**
   v2 flattened the `Microsoft.OpenApi.Models` namespace to `Microsoft.OpenApi`,
   replaced `OpenApiReference`/`ReferenceType.SecurityScheme` with
   `OpenApiSecuritySchemeReference(id, document)`, and made several members
   (`Components`, `Security`, `SecuritySchemes`) nullable-annotated — the
   transformer carries `??=` guards that warnings-as-errors demanded.
5. **Readiness checks are opt-in by tag; the SQL check is only registered when
   a connection string exists.** The SqlServer health check *throws* for an
   empty connection string, and it throws in the DI factory delegate — outside
   the check's own try/catch — so it escapes as a 500 instead of an Unhealthy
   report. Conditional registration fixes that and also fixed the same latent
   bug in the old `/health` endpoint (nothing ever polled it, so nobody
   noticed).
6. **Fixed-window limiter, 10/min/IP, no queue.**
   Fixed window is the simplest correct choice for brute-force protection;
   `QueueLimit = 0` means excess requests are rejected immediately rather than
   piling up. Keyed by client IP (`RemoteIpAddress`); behind a proxy this needs
   `ForwardedHeadersMiddleware` — noted as the follow-up, not done here.
7. **Problem `type` URIs use `https://httpstatuses.com/{code}`.**
   RFC 9457 wants a URI identifying the problem type; these are stable,
   human-readable, and unambiguous for a portfolio API. Production 500s still
   carry no exception detail (leak check is a unit test).

## 3. Verification (same machine, Release)

- Build: 0 warnings, 0 errors (warnings-as-errors is on).
- Unit: 14/14 (10 existing + 4 new `GlobalExceptionHandlerTests`: media type,
  `type` URIs, `errors` extension shape, no-leak in Production).
- Integration: 9 passed + 3 pre-existing skips (5 existing + 4 new
  `HealthEndpointTests`: `/health/live` → 200, `/health/ready` → 200 with no DB
  configured, → 503 with an unreachable DB via `UseSetting` override,
  legacy `/health` → 404).
- Live smoke test: `/openapi/v1.json` → 200 with the Bearer scheme and
  document-level security requirement; `/scalar/v1` → 200; 12 rapid
  `/api/auth/login` calls → 10× 400 then 2× 429 (limiter trips exactly at the
  permit limit).
- Note: integration tests need the same `Jwt__Key`/`Jwt__Issuer`/`Jwt__Audience`
  env vars CI sets — without them the app NREs at startup on `main` too
  (pre-existing, unrelated to this PR).

## 4. Interview Q&A

**Q1. Why replace Swashbuckle with the built-in OpenAPI support?**
Swashbuckle was the community standard before ASP.NET Core shipped its own
document generator. The built-in `Microsoft.AspNetCore.OpenApi` removes a
third-party dependency from the stack, tracks the framework's release train,
and uses the Microsoft-maintained OpenAPI object model. The migration was
mechanical: same `ApiExplorer` metadata in, same JSON document out — verified
by diffing the generated document's paths and security schemes.

**Q2. What is an OpenAPI document transformer, and why did you need one?**
Transformers are hooks that mutate the generated document — the supported
replacement for Swashbuckle's `AddSecurityDefinition`/`AddSecurityRequirement`
and operation filters. The framework can't know our auth scheme, so
`BearerSecuritySchemeTransformer` adds the JWT Bearer definition and marks the
whole document as requiring it, which is what lights up the Authorize button
in Scalar.

**Q3. What's the difference between liveness and readiness probes?**
Liveness asks "is the process alive?" — failure restarts the container.
Readiness asks "can this instance serve traffic right now?" — failure removes
it from load-balancer rotation *without* restarting it. Conflating them means a
transient DB outage recycles all your pods at once instead of just shedding
load until the database recovers.

**Q4. Why does the readiness check use tags?**
`MapHealthChecks` takes a predicate over registered checks. Tagging the SQL
check `"ready"` and filtering on it means liveness (`Predicate = _ => false`,
no checks run) and readiness select different subsets from one registry. New
dependency checks just get the tag — no endpoint changes.

**Q5. Why is the SQL health check registered conditionally?**
The SqlServer check throws `ArgumentNullException` for an empty connection
string, and it throws inside the DI factory delegate — which runs *outside*
the per-check try/catch in this library version. So a misconfigured check
escapes as an HTTP 500 instead of an Unhealthy report. Only registering it
when a connection string exists turns "nothing to probe" into a clean 200
instead of a misleading 500. The old single `/health` endpoint had the same
latent bug.

**Q6. Why rate-limit the auth endpoints specifically?**
Login, register and refresh are the brute-force/credential-stuffing surface:
they're unauthenticated, cheap to call, and each attempt has a small chance of
success for an attacker with a password list. 10/minute/IP makes large-scale
guessing infeasible while never troubling a human. The 429 includes
`Retry-After` so well-behaved clients back off.

**Q7. Fixed window vs sliding window vs token bucket — why fixed?**
Fixed window is the simplest correct limiter for abuse protection: easy to
reason about, cheap (one counter per key per window), and the boundary-burst
weakness (20 requests straddling a window edge) doesn't matter when the goal
is making bulk guessing uneconomical rather than precise shaping. I'd reach
for sliding window when fairness at the boundary matters (e.g. per-customer
API quotas).

**Q8. What breaks when you put this behind a load balancer?**
`RemoteIpAddress` becomes the load balancer's IP, so every client shares one
bucket — one abusive user throttles everyone. The fix is
`ForwardedHeadersMiddleware` (processing `X-Forwarded-For` from trusted
proxies only) before the rate limiter. Deliberately left for the Azure
milestone, where the topology is known.

**Q9. What does RFC 9457 require, and what was wrong before?**
RFC 9457 (successor to 7807) defines `application/problem+json`: a response
with `type` (URI identifying the problem), `title`, `status`, `detail`, and
`instance`. We were returning `application/json` with no `type` — clients
couldn't reliably distinguish our error kinds by media type or identifier.
Now the media type is correct and every problem carries a stable `type` URI.

**Q10. How do you test a 503 readiness probe without a real database?**
Two tests: with no connection string configured, no check is registered and
the endpoint is trivially 200 (documents the conditional registration);
with `WebApplicationFactory.WithWebHostBuilder` + `UseSetting` pointing at
`127.0.0.1:1433` (nothing listens — connection refused, fast and
deterministic), the check registers, fails, and the endpoint returns 503.
That proves the gate actually trips instead of just existing.

## 5. Exercise (15 minutes)

Do this against the running API before merging:

1. Start the API in Development and open `/scalar/v1`. Use the Authorize
   button with any string as the token, then call `GET /api/jobapplications`
   — observe the 401 and its `application/problem+json` body. Write down the
   `type` value.
2. In a terminal, POST 12 times in quick succession to
   `/api/auth/login` with `{"email":"x","password":"y"}`. Note which request
   number first returns 429, and check the `Retry-After` header.
3. In one paragraph, answer: the limiter is keyed by client IP — what breaks
   when this API sits behind a load balancer or a corporate NAT, and what
   middleware fixes it?
