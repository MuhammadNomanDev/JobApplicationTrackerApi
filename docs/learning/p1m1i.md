# P1/M1i — Infrastructure service coverage: learning pack

Milestone: cover the infrastructure services (JWT, password hashing, blob
storage, Service Bus) with unit tests. 15 new tests; suite 99 → 114 unit
tests, all green. Local coverage 39.4% → **43.0%** (+3.6pp). Ratchet 39 → 43.

## What changed

Production (two minimal test seams; DI behaviour unchanged):
- `BlobStorageService`: secondary public constructor
  `(BlobServiceClient, string containerName, string connectionString)`.
- `ServiceBusMessagePublisher`: secondary public constructor
  `(ServiceBusSender)`.
- **Bug fix** `BlobStorageService.ExtractAccountKey` (and `ExtractAccountName`):
  `Split('=')[1]` stripped base64 `=` padding — every real Azure storage key
  ends with padding, so `GetSasUrlAsync` threw `FormatException`
  (500 on document download). Now uses `Substring` past the key prefix.

Tests (`tests/.../Services/`):
- `JwtTokenGeneratorTests` (4): claim contents (NameIdentifier/Email/GivenName/
  Surname), issuer/audience, expiry math, refresh-token uniqueness, setting
  passthrough.
- `PasswordHasherTests` (3): hash/verify round-trip, wrong password, salt
  uniqueness (BCrypt work factor 12).
- `BlobStorageServiceTests` (4): upload (container create + content type +
  returned URI), delete (blob name extracted from URL), SAS URL signing
  (real service, fake connection string — no network), missing connection
  string throws.
- `ServiceBusMessagePublisherTests` (4): unconfigured publish/dispose are
  graceful no-ops; configured publish sends JSON with ContentType, Subject
  (= event type name) and MessageId; dispose disposes the sender.

## Q&A

### 1. Why did the services need constructor seams?
Both constructed their Azure SDK clients internally (`new BlobServiceClient(...)`,
`new ServiceBusClient(...)`), leaving no way to substitute a fake. The
secondary constructors inject pre-built clients. MS DI still picks the
`IConfiguration` constructor (it chooses the greediest *resolvable*
constructor; the SDK clients aren't registered), so runtime behaviour is
unchanged — the integration tests (full DI container) confirm it.

### 2. Why hand-written fakes instead of Moq for the blob clients?
Moq couldn't disambiguate the Azure SDK overloads in expression trees:
`CreateIfNotExistsAsync` has 4-arg and 3-arg overloads, and expression trees
reject out-of-position named arguments (CS9307). Hand-written fakes overriding
the exact virtual signatures are unambiguous and also record calls for
assertion. (Probed first: every member needed — `GetBlobContainerClient`,
`GetBlobClient`, `UploadAsync`, `DeleteIfExistsAsync`, `Uri` — is virtual.)

### 3. What was the `ExtractAccountKey` bug?
`accountKeyPart.Split('=')[1]` on `"AccountKey=ABC...=="` returns the key
*without* its trailing `=` padding. `StorageSharedKeyCredential` then threw
`FormatException` inside `GetSasUrlAsync` — every document download that
minted a SAS URL would have 500'd against a real storage account, since all
real keys are padded base64. The new SAS test (fake key with padding) caught
it; fix is `accountKeyPart["AccountKey=".Length..]`.

### 4. How is `GetSasUrlAsync` tested without network?
`BlobClient.GenerateSasUri` signs locally — no HTTP involved. The test builds
the real service with a fake-but-well-formed connection string and asserts the
URL carries `sig=`, `sr=b`, `se=`. This also covers the private
`ExtractAccountName`/`ExtractAccountKey` through the public path.

### 5. How do you test the unconfigured Service Bus path?
Build the publisher with empty config: `_isConfigured` is false, `_sender` is
null, and `PublishAsync` returns without touching the network. Both publish
and dispose are asserted not to throw — this is the dev-machine path.

### 6. Why parse the JWT instead of validating it in tests?
`JwtSecurityTokenHandler.ReadJwtToken` parses without validation — enough to
assert the claims the API depends on (`ClaimTypes.NameIdentifier`, which
`CreateJobApplicationCommandHandler` reads via `IHttpContextAccessor`).
Validation (signature/lifetime) is integration-test territory.

### 7. Why is `TokenExpirationMinutes` a test?
It's a one-line passthrough, but it pins the contract the refresh handler
relies on (`expiration = UtcNow.AddMinutes(generator.TokenExpirationMinutes)`).
Cheap test, real coupling.

### 8. What's still uncovered in Infrastructure?
`NotificationConsumer` (background service, 74 lines) — needs a hosted-service
harness; `AppDbContext`/`UnitOfWork`/repositories are covered by the
Testcontainers suite in CI. M1j covers the API layer.

### 9. Did the seams change the public API?
Two additive constructors with XML doc comments noting they are test seams.
No existing constructor changed; no DI registration changed.

### 10. Coverage math check
2154/5004 = 43.0% locally (excl. Docker DB tests). CI runs the Testcontainers
suite too, so expect ~48–52% there; raise the ratchet to the CI number
pre-merge per the M1g/M1h pattern.

## Exercise (15–20 min, do before merging)

1. In `BlobStorageServiceTests`, temporarily revert the `ExtractAccountKey`
   fix (back to `Split('=')[1]`) and run `GetSasUrlAsync_ReturnsSignedReadUrl`.
   Confirm it fails with `FormatException`, then restore the fix.
2. Add a test: `ServiceBusMessagePublisher` with a connection string but no
   queue name → publish is a graceful no-op (covers the other half of the
   `_isConfigured` branch).
3. Explain out loud: why does MS DI still choose the `IConfiguration`
   constructor after you added a second public constructor?
