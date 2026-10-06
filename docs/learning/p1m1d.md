# P1/M1d — HybridCache replaces RedisCacheService

## 1. What changed and why

`RedisCacheService` (hand-rolled over StackExchange.Redis) is deleted.
`HybridCacheService` implements the same application-layer `ICacheService`
on top of the framework's `HybridCache`:

- **In-memory L1 always.** Even with no Redis configured, reads now hit a
  local cache. The old service *silently no-op'd* without Redis — every call
  went to the database and nobody could tell from the code.
- **Redis as L2 when configured.** `AddStackExchangeRedisCache` registers the
  `IDistributedCache` that HybridCache picks up as its second level. One
  instance warms the cache for all instances.
- **Stampede protection.** `GetOrCreateAsync` guarantees the factory runs at
  most once per key under concurrency — the thundering-herd problem the old
  get-then-set pattern had is gone by construction.
- **Tag invalidation instead of `KEYS` scans.** The old
  `RemoveByPrefixAsync("jobapps:")` ran Redis `KEYS "jobapps:*"` — an O(N)
  scan over the whole keyspace, the command every Redis guide tells you never
  to run in production. HybridCache has no prefix scan at all; entries are
  tagged `"jobapps"` at write time and invalidated with `RemoveByTagAsync`.

## 2. Decisions

1. **`Microsoft.Extensions.Caching.Hybrid` 10.10.0**,
   **`Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.12**,
   both pinned in `Directory.Packages.props`. The direct `StackExchange.Redis`
   reference is gone (nothing else used it).
2. **The interface was reshaped, not just re-implemented.**
   `GetAsync`/`SetAsync`/`RemoveByPrefixAsync` became
   `GetOrCreateAsync`/`RemoveAsync`/`RemoveByTagAsync`. Keeping the old shape
   would have meant reimplementing prefix semantics on top of tags — a lie in
   the API. The honest rename touched 4 handlers and 1 test; all covered.
3. **Cache-aside moved into the handler via `GetOrCreateAsync`.**
   `GetAllJobApplicationsQueryHandler` now passes its query as the factory
   with `tags: ["jobapps"]` and a 5-minute TTL, instead of get → null-check →
   query → set. Fewer lines, no check-then-act race, stampede protection free.
4. **Conditional L2 registration mirrors the M1c health-check lesson.**
   `AddStackExchangeRedisCache` only runs when `Redis:ConnectionString` is
   set — same "don't probe what isn't configured" principle as the readiness
   check.
5. **Tags are a write-time concern.** The tag is supplied on `Set` (inside
   `GetOrCreateAsync`); invalidation only names the tag. If a future handler
   forgets the tag, its entries survive invalidation — a code-review catch,
   covered for the current handlers by the tag test.

## 3. Verification (same machine, Release)

- Build: 0 warnings, 0 errors.
- Unit: 19/19 (14 existing + 5 new `HybridCacheServiceTests` against a real
  `HybridCache` with no Redis: factory-runs-once, 20-way concurrent
  thundering-herd → 1 factory call, remove evicts, tag invalidation evicts
  only the tagged group, local-only operation without Redis).
- Integration: 9 passed + 3 pre-existing skips — the handler reshapes broke
  nothing.

## 4. Interview Q&A

**Q1. What is HybridCache and why replace a hand-rolled Redis client with it?**
HybridCache is the framework's two-level cache: fast in-memory L1 in front of
a shared L2 (Redis), with one API. The hand-rolled service only knew Redis
and no-op'd without it; HybridCache degrades to a real local cache instead of
no cache, and it brings stampede protection and tag invalidation that we'd
otherwise reimplement badly.

**Q2. What is the thundering-herd / cache-stampede problem?**
When a hot key expires, N concurrent requests all miss, all run the expensive
factory (here: a database query), and all write back. `GetOrCreateAsync`
serialises the factory per key: one execution serves all waiters. The old
get-then-set pattern had the race by construction — the new test proves 20
concurrent callers trigger exactly 1 factory run.

**Q3. Why is `KEYS "prefix:*"` bad, and what replaced it?**
`KEYS` scans the entire keyspace — O(N) — and blocks the Redis event loop
while it runs; on a production instance it's a self-inflicted outage. The
replacement is tag invalidation: entries are labelled at write time and the
whole label is dropped in O(tagged entries). No scan, no blocking.

**Q4. Why change the `ICacheService` interface instead of just the implementation?**
An interface should describe what the system can actually do. Prefix scanning
isn't something HybridCache supports, so keeping `RemoveByPrefixAsync` would
mean faking it (e.g. tracking keys locally — broken across instances) or
lying about the semantics. The rename to `RemoveByTagAsync` makes the real
capability visible and keeps every implementation honest.

**Q5. How does the L1/L2 interaction work on a write?**
`GetOrCreateAsync` checks L1, then L2, then runs the factory; the result is
written to both levels. A second app instance finds it in L2 (Redis) and
promotes it to its own L1. Invalidation (`RemoveByTagAsync`) clears both.

**Q6. What happens when Redis is down or unconfigured?**
Unconfigured: pure in-memory L1, everything works, nothing is shared between
instances. Down at runtime: HybridCache treats L2 failures as a miss and
serves from L1/factory rather than throwing — the app degrades to local
caching instead of erroring. That behaviour is by design, not accident.

**Q7. Why `GetOrCreateAsync` instead of separate get/set?**
Separate get/set has a check-then-act race (two callers both miss, both
query, both write) and pushes the "did I miss?" logic into every caller. The
factory form centralises it: one call site, one code path, stampede
protection included. The handler went from ~10 lines of caching ceremony to 5.

**Q8. How do the tags get onto the entries?**
At write time: `GetOrCreateAsync(key, factory, ttl, tags: ["jobapps"])`.
Invalidation names only the tag. Forgetting the tag on a new cached query
means its entries survive invalidation — that's the code-review checklist
item for any future cached handler.

**Q9. What did you verify about serialisation?**
HybridCache serialises values itself (JSON by default). The cached DTOs
(`PagedResult<JobApplicationDto>`) were already JSON-serialised by the old
service, so the requirement didn't change — but it's now the framework's
concern, not ours, and the round-trip is covered by the tests.

**Q10. When would you NOT use HybridCache?**
When you need Redis data structures (sorted sets, streams, pub/sub), Lua
scripts, or fine-grained TTL/ eviction control per key beyond what entry
options offer — HybridCache is a cache, not a Redis client. Anything using
Redis as a database or message bus stays on StackExchange.Redis directly.

## 5. Exercise (15 minutes)

1. In `HybridCacheServiceTests`, add a test: create key `"t"` with tag
   `"g1"`, call `RemoveAsync("t")` (not by tag), then `GetOrCreateAsync`
   for `"t"` again — assert the factory runs again. Then tag a second key
   `"u"` with `"g1"`, call `RemoveByTagAsync("g1")`, and assert *both* `"t"`
   (recreated after the single remove) and `"u"` are refetched. This nails
   the difference between key removal and tag invalidation.
2. In one paragraph: the old service no-op'd without Redis. Describe a
   production incident that behaviour could cause (hint: what does "cache"
   mean for database load when it silently isn't there?), and why failing
   loudly or degrading to L1 are both better.
