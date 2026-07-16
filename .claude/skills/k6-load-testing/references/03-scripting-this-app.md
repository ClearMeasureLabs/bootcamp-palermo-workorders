# Module 3 — Scripting This App (bootcamp-palermo-workorders)

Standards: Grafana k6 HTTP requests / checks / `http.file` (multipart) reference.
This module is stack-specific — it maps the app's actual HTTP surface to k6
scripts. For a different target, rebuild this inventory first (see the pipeline
in `SKILL.md`).

## Endpoint inventory (k6 targets)

| Method | Route | Auth-gated when key on? | Rate-limited (prod)? | Success | Notes |
|---|---|---|---|---|---|
| GET | `/api/ping` | No (always open) | Yes | 200 `text/plain` `pong` | cheapest baseline target |
| GET | `/api/time` | No (always open) | Yes | 200 text / 304 | weak ETag |
| GET | `/api/version` | No (always open) | Yes | 200 JSON / 304 | output-cached 10m |
| GET | `/api/health`, `/api/health/detailed` | Yes | Yes | 200 JSON / 304 | |
| GET | `/api/diagnostics` | Yes | Yes | 200 JSON / 304 | |
| GET | `/api/v1.0/WeatherForecast` | Yes | Yes | 200 JSON / 304 | 5-item array |
| POST | `/api/work-orders/bulk-import` | Yes | Yes | 200 JSON | multipart `file` (CSV); realistic write load |
| POST | `/api/blazor-wasm-single-api` | Yes | **No** (no attribute) | 200 JSON-string | MediatR envelope; realistic DB read/write, un-throttled |

Baseline latency/throughput → the open GETs (`/api/ping`, `/api/time`,
`/api/version`). Realistic DB-backed load → `bulk-import` (writes) and
`blazor-wasm-single-api` (reads/commands). The single-api path is **not**
rate-limited, so it's the right target for capacity numbers that shouldn't be
capped at 100/60s (see Module 4).

## Anti-patterns to hunt (when scripting)

| Pattern | Why it hurts | Evidence to look for |
|---|---|---|
| No `check()` on status/body | A fast error counts as a fast success; latency numbers become meaningless | `http.get(...)` with no following `check(res, {...})` |
| Hardcoded base URL / API key in the script | Not portable across local/deployed; leaks a key into git | literal `https://localhost:7174` or a key string instead of `__ENV.BASE_URL` / `__ENV.API_KEY` |
| Wrong header casing for the API key | `X-Api-Key` (auth) vs `X-API-Key` (rate-limit partition) are different headers | sending only one when both matter |
| Guessed `WebServiceMessage` type names | The server deserializes by assembly-qualified name; a typo yields a 500, not a domain error | `TypeName` without the `, Core` assembly suffix |
| Reusing the localhost dev cert without `--insecure-skip-tls-verify` | k6 aborts on the self-signed cert; every request "fails" | TLS errors in k6 output against `https://localhost` |

## Authentication & TLS

- API-key header is **`X-Api-Key`** (config `ApiKeyAuthentication:Enabled` +
  `:ValidationKey`; **off by default**). Only `/api/*` paths are gated; `ping`,
  `time`, `version` stay open even when the key is on. Supply via
  `__ENV.API_KEY` and send it on every gated request.
- Rate-limit **partition** header is separately **`X-API-Key`** (note casing) —
  set it to spread or isolate buckets when testing the limiter (Module 4).
- Localhost uses a self-signed dev cert: run with
  `k6 run --insecure-skip-tls-verify …` (local only — never against deployed).

## Worked k6 skeleton — baseline GETs

```js
import http from 'k6/http';
import { check } from 'k6';

const BASE = __ENV.BASE_URL || 'https://localhost:7174';
const PROFILE = __ENV.PROFILE || 'smoke';

const SCENARIOS = {
  smoke: { executor: 'constant-vus', vus: 2, duration: '30s' },
  load:  { executor: 'ramping-arrival-rate', startRate: 10, timeUnit: '1s',
           preAllocatedVUs: 50, maxVUs: 200,
           stages: [{ target: 50, duration: '1m' }, { target: 50, duration: '5m' }] },
  spike: { executor: 'ramping-arrival-rate', startRate: 10, timeUnit: '1s',
           preAllocatedVUs: 50, maxVUs: 500,
           stages: [{ target: 10, duration: '10s' }, { target: 300, duration: '10s' },
                    { target: 300, duration: '30s' }, { target: 10, duration: '10s' }] },
};

export const options = {
  scenarios: { [PROFILE]: SCENARIOS[PROFILE] },
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed:   ['rate<0.01'],
    checks:            ['rate>0.99'],
  },
};

export default function () {
  const res = http.get(`${BASE}/api/ping`);
  check(res, {
    'status is 200': (r) => r.status === 200,
    'body is pong':  (r) => r.body === 'pong',
  });
}
```

Run: `PROFILE=smoke k6 run --insecure-skip-tls-verify k6/baseline.js`

## Worked k6 skeleton — domain read via `blazor-wasm-single-api`

The endpoint takes JSON `WebServiceMessage { TypeName, Body }`; `TypeName` is an
assembly-qualified .NET type name, `Body` is the JSON of the inner request. The
response is a JSON *string* that is itself a serialized `WebServiceMessage`.

```js
import http from 'k6/http';
import { check } from 'k6';

const BASE = __ENV.BASE_URL || 'https://localhost:7174';
const API_KEY = __ENV.API_KEY || '';

// Assembly-qualified names (verify against src/Core/Queries before running):
const T = {
  employeeGetAll: 'ClearMeasure.Bootcamp.Core.Queries.EmployeeGetAllQuery, Core',
  workOrderByNumber: 'ClearMeasure.Bootcamp.Core.Queries.WorkOrderByNumberQuery, Core',
};

export const options = {
  scenarios: { load: { executor: 'ramping-arrival-rate', startRate: 5, timeUnit: '1s',
    preAllocatedVUs: 50, maxVUs: 200,
    stages: [{ target: 40, duration: '1m' }, { target: 40, duration: '3m' }] } },
  thresholds: { http_req_duration: ['p(95)<800'], http_req_failed: ['rate<0.02'], checks: ['rate>0.98'] },
};

export default function () {
  const envelope = JSON.stringify({
    TypeName: T.employeeGetAll,
    Body: JSON.stringify({}),          // EmployeeGetAllQuery has no fields
  });
  const headers = { 'Content-Type': 'application/json' };
  if (API_KEY) headers['X-Api-Key'] = API_KEY;

  const res = http.post(`${BASE}/api/blazor-wasm-single-api`, envelope, { headers });
  check(res, {
    'status is 200': (r) => r.status === 200,
    'has envelope body': (r) => r.body && r.body.length > 0,
  });
}
```

## Worked k6 skeleton — write load via `bulk-import` (multipart CSV)

`CreatorUsername` must be a seeded employee that can create (e.g. `jpalermo`).
`RoomNumber` is free-text and optional. `Number` is server-generated — never
supplied. Seed usernames from the DbUp seed / `ZDataLoader`: `jpalermo`,
`jborys`, `mpeck`, `bsides`, `csullivan`, etc.

```js
import http from 'k6/http';
import { check } from 'k6';

const BASE = __ENV.BASE_URL || 'https://localhost:7174';
const API_KEY = __ENV.API_KEY || '';

const csv = [
  'Title,Description,CreatorUsername,RoomNumber',
  'Broken window,Glass cracked in the east wall,jpalermo,Sanctuary',
  'Leaking tap,Drip in the kitchen sink,jpalermo,Church Office',
].join('\n');

export const options = {
  scenarios: { load: { executor: 'constant-arrival-rate', rate: 5, timeUnit: '1s',
    duration: '2m', preAllocatedVUs: 20, maxVUs: 50 } },
  thresholds: { http_req_duration: ['p(95)<1500'], http_req_failed: ['rate<0.02'], checks: ['rate>0.98'] },
};

export default function () {
  const headers = {};
  if (API_KEY) headers['X-Api-Key'] = API_KEY;
  const res = http.post(`${BASE}/api/work-orders/bulk-import`,
    { file: http.file(csv, 'load.csv', 'text/csv') }, { headers });
  check(res, {
    'status is 200': (r) => r.status === 200,
    'created rows': (r) => { try { return JSON.parse(r.body).createdCount >= 1; } catch { return false; } },
  });
}
```

Note: `bulk-import` writes real rows — always run it against the throwaway
SQLite DB (Module 5), never a shared/real database.

## Detection commands

```
# Confirm the target is up and the baseline endpoint works before any load:
curl -sk https://localhost:7174/_healthcheck
curl -sk https://localhost:7174/api/ping        # expect: pong

# Verify assembly-qualified type names against the source before scripting single-api:
grep -rniE "class .*Query|record .*Query" src/Core/Queries
```

Cross-reference Module 4 (rate-limit behaviour on these routes), Module 5
(bringing the target up with a throwaway DB + seed data), and Module 6
(capturing the results).
