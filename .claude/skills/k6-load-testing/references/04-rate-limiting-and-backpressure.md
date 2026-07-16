# Module 4 — Rate Limiting & Back-Pressure

Standards: Grafana k6 custom-metrics (`Rate`) & response-handling reference;
HTTP 429 semantics (RFC 6585 §4, `Retry-After`).

This app has a **custom sliding-window limiter** that materially changes how a
load test's numbers must be read. Getting this wrong is the most common
load-testing false-finding: reporting the limiter *doing its job* as a defect.

## The limiter, as configured

| Property | Value | Source |
|---|---|---|
| Policy | `ApiSlidingWindow` | `src/UI/Api/ApiRateLimiting.cs` |
| Limit | **100 requests / 60 s** per client, 4 segments, `QueueLimit=0` (fail fast) | `src/UI/Server/ApiRateLimitingOptions.cs`, `appsettings.json` |
| Partition key | `X-API-Key` header → else `User.Identity.Name` → else remote IP → else `anonymous` | `src/UI/Server/ApiRateLimitingExtensions.cs` |
| Throttled response | **HTTP 429**, `text/plain`, headers `Retry-After`, `X-RateLimit-{Limit,Remaining,Reset}` | `src/UI/Server/RateLimiting/RateLimitingMiddleware.cs` |
| Scope | endpoints with `[EnableRateLimiting]` under `/api` — Ping, Time, Version, Health, Diagnostics, WeatherForecast, bulk-import | (attribute-gated) |
| **Not** limited | `/api/blazor-wasm-single-api` (no attribute), `/mcp`, gRPC | |
| **Default state** | **DISABLED in Development**; enabled (100/60s) in base `appsettings.json` (prod/other) | `appsettings.Development.json` |

## Anti-patterns to hunt

| Pattern | Why it hurts | Evidence to look for |
|---|---|---|
| Counting expected 429s as `http_req_failed` | Back-pressure inflates the error rate; a healthy limiter fails the run | no 429 carve-out; error rate jumps exactly at 100 req/60s per client |
| Reporting "the API errors under load" when it's the limiter | Mislabels a working control as a defect | 429s (not 5xx) dominating the "failures" |
| Testing throughput on a rate-limited route and reporting the cap as "max capacity" | The number is the limiter's ceiling, not the app's | RPS plateaus at ~1.67/s (100/60s) on `/api/ping` with one partition |
| One partition (anonymous/IP) for a throughput test | All VUs share one 100/60s bucket, so real capacity is masked | no `X-API-Key` variation; single IP |
| Testing the limiter with it disabled (Development) | You measure nothing — no ceiling exists | limiter off but the report claims to validate it |

## What "good" looks like

- **Decide the intent first**, and state it in the report:
  - *Measuring app capacity/latency* → target the **un-throttled**
    `/api/blazor-wasm-single-api`, or enable a high/known limit, or spread
    partitions so the limiter isn't the bottleneck. State that the limiter was
    bypassed/raised and why.
  - *Validating the limiter itself* → enable it
    (`ApiRateLimiting__Enabled=true`, run in a non-Development environment),
    keep **one** partition, drive > 100 req/60s, and assert the *expected* shape:
    the first ~100 succeed, the rest return 429 with correct `Retry-After` /
    `X-RateLimit-*` headers.
- **Categorize 429 separately from failure** using a custom `Rate`:
  ```js
  import http from 'k6/http';
  import { check } from 'k6';
  import { Rate } from 'k6/metrics';

  const throttled = new Rate('throttled_429');   // designed back-pressure
  const realErrors = new Rate('real_errors');    // genuine failures

  export const options = {
    thresholds: {
      real_errors: ['rate<0.01'],        // gate on REAL errors only
      // throttled_429 is observed, not gated (it's expected when testing the limiter)
    },
  };

  export default function () {
    const res = http.get(`${__ENV.BASE_URL}/api/ping`);
    throttled.add(res.status === 429);
    realErrors.add(res.status >= 500 || res.status === 0);
    check(res, { 'ok or throttled': (r) => r.status === 200 || r.status === 429 });
  }
  ```
- Honour `Retry-After` in closed-model scripts so the client backs off like a
  real one, rather than hammering a saturated bucket.

## Detection commands

```
# Enable the limiter locally to exercise the 429 path (otherwise off in Dev):
ASPNETCORE_ENVIRONMENT=Production ApiRateLimiting__Enabled=true dotnet run --project src/UI/Server

# Confirm the 429 shape by hand (101st request within a window):
for i in $(seq 1 105); do curl -sk -o /dev/null -w "%{http_code}\n" https://localhost:7174/api/ping; done | sort | uniq -c
```

Cross-reference Module 2 (gate on real errors, not 429), Module 3 (which routes
carry the limiter), and Module 5 (setting the environment/config that toggles it).
