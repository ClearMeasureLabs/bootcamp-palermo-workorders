---
name: otel-observability-mindset
description: >-
  Keeps OpenTelemetry-driven observability and operability quality attributes
  top of mind while writing or reviewing code in this repo. Load this
  whenever you add or touch a MediatR query/handler, an IStateCommand, a call
  to an external dependency (HTTP, SQL, LLM, NServiceBus), a background
  worker/endpoint, or a user-facing error path — and whenever the user asks
  about tracing, metrics, logging, telemetry, monitoring, health checks, or
  "how do we see this in production." The app already has real OpenTelemetry
  wiring (ActivitySources, a Meter, Serilog-to-OTel bridge, OTLP/Azure Monitor
  exporters) — use this skill so new code extends that wiring instead of
  reinventing it or silently falling outside it.
---

# OpenTelemetry Observability Mindset

This app is not starting an OpenTelemetry setup from scratch — it already has
one, wired centrally in
[Extensions.cs](../../../src/ChurchBulletin.ServiceDefaults/Extensions.cs) and
demonstrated in three real components:
[Bus.cs](../../../src/UI.Shared/Bus.cs) (MediatR boundary, traces),
[TracingChatClient.cs](../../../src/LlmGateway/TracingChatClient.cs) (external
dependency boundary, traces), and
[TelemetryHandler.cs](../../../src/DataAccess/Handlers/TelemetryHandler.cs)
(business metric via domain event). The job when writing new code is almost
never "add OpenTelemetry" — it's "extend the existing wiring the way these
three already do," and remember the one step that's easy to forget:
registering anything new in `ServiceDefaults`.

## The mental model

- **Traces**: each observable component owns one `static readonly
  ActivitySource` named `ChurchBulletin.<Component>` (sub-boundaries use a
  dot, e.g. `ChurchBulletin.Application.Bus`), version `"1.0.0"`. Every
  `ActivitySource` in the app is enumerated by name in
  `ConfigureOpenTelemetry()` via `.AddSource(...)`.
- **Metrics**: one shared `Meter` today, `"ChurchBulletin.Application"`,
  registered via `.AddMeter(...)` alongside ASP.NET Core / HttpClient /
  runtime instrumentation.
- **Logs**: Serilog is the logging pipeline of record (compact JSON to
  stdout, container-friendly) and forwards to every other `ILoggerProvider`
  — including the OpenTelemetry logging bridge — via `writeToProviders:
  true`. Don't add a second logging pipeline; use `ILogger` + `LogContext`
  scopes and let Serilog fan it out.
- **Exporters**: OTLP and Azure Monitor are both wired and enabled purely by
  config presence (`OTEL_EXPORTER_OTLP_ENDPOINT`,
  `ApplicationInsights:ConnectionString`) — either, both, or neither. In
  Development, `LocalTelemetryFileWriter` also runs (alongside any configured
  exporters, not just as a fallback) so telemetry is always visible locally.
  New code never needs to touch exporter selection.
- **Health/operability**: the operative health endpoints are `/_healthcheck`
  and `/_healthcheck/detailed`, mapped in `src/UI/Server/Program.cs` and
  backed by the real dependency checks registered in
  `src/UI/Server/UIServiceRegistry.cs` (LLM gateway, database, etc.).
  ServiceDefaults additionally maps `/health` and `/alive` with only a
  trivial `"self"` check. Request logging is already on. The question for
  new code is narrower: does this specific new dependency need its own
  health check.

Read `src/ChurchBulletin.ServiceDefaults/Extensions.cs` before adding
anything at the wiring level — it's short and is the actual source of truth,
not this summary.

## Checklist — run this whenever code crosses an observable boundary

A "boundary" is any of: a new MediatR query/command handler, a new
`IStateCommand`, a call out to HTTP/SQL/an LLM/NServiceBus, a new background
worker or endpoint, or a user-facing flow with a failure path. Not every
private method needs a span — reserve `ActivitySource` use for boundaries
that someone debugging production would actually want to see.

1. **Reuse before inventing.** Is there already an `ActivitySource` for this
   component? Bus operations already get a span from `Bus.cs` for anything
   sent through `IBus` — a new MediatR handler usually doesn't need its own
   `ActivitySource`, it inherits the Bus span as its parent. For a one-off
   application-level span with no new component boundary, reuse the public
   `Extensions.ApplicationActivitySource` (`"ChurchBulletin.Application"`,
   already registered). Only add a new `ActivitySource` when instrumenting a
   genuinely new component boundary (a new external dependency, a new
   subsystem), not per-handler.
2. **If a new `ActivitySource` or `Meter` name is genuinely needed, register
   it in `ServiceDefaults/Extensions.cs`** (`.AddSource("...")` /
   `.AddMeter("...")`) in the same change. An unregistered source compiles
   fine and silently produces spans that never reach any exporter — this is
   the single most common way new instrumentation goes missing. Treat adding
   a source/meter without touching `Extensions.cs` as incomplete work. The
   same rule applies one level up and to libraries: a **host** that never
   calls `AddServiceDefaults()`/`ConfigureOpenTelemetry()` drops every span
   and metric produced inside it no matter how well-instrumented the shared
   code is (check `Program.cs` when adding or touching a host), and turning
   on a library's metrics (e.g. NServiceBus `EnableMetrics`) does nothing
   until that library's meter name is also in `.AddMeter(...)`.
3. **Name it `ChurchBulletin.<Component>`** (dot-separated sub-boundaries for
   more specific scopes), version `"1.0.0"`, matching the existing sources.
4. **Follow the span pattern from `Bus.cs`**: continue the current trace
   context when one exists (`Activity.Current?.Context`), name the span
   `"{Component}.{Operation} {Subject}"`, tag with a consistent
   `component.field` prefix, wrap the real call in try/catch, and on
   exception set `ActivityStatusCode.Error` plus `error`, `exception.type`,
   `exception.message`, `exception.stacktrace` tags — then **always
   rethrow**. A tracing wrapper must never swallow or alter an exception; its
   only job is to observe. (This tag set is the canonical error pattern —
   it matches OTel semantic conventions. `TracingChatClient.cs` records an
   `ActivityEvent` instead; prefer the `Bus.cs` tags for new code.)
5. **For a wrapped external dependency (new HTTP client, new provider,
   etc.), follow the decorator pattern from `TracingChatClient.cs`**: wrap
   the real client/interface, add `ActivityEvent`s for the meaningful
   moments (request sent, response received), tag provider/operation
   identifiers, and apply the same catch-tag-rethrow rule — including inside
   any `IAsyncEnumerable` streaming path, where a mid-stream exception still
   needs to land on the activity before it propagates. Copy its structure,
   **not** its `chat.prompt`/`chat.response` tags — those put full payload
   text on spans, which rule 7 below says to avoid; they predate this
   guidance and are known tech debt, not a pattern to repeat.
6. **Add a metric, not just a trace, for anything business-relevant** —
   a work order created, a status transition, a failed fulfillment attempt.
   Traces answer "what happened in this one request"; metrics answer "how
   often, and is the rate changing" without anyone needing to go trace
   spelunking. Follow `TelemetryHandler.cs`: the domain raises an event,
   `IBus.Publish` fans it out, and a notification handler owns the
   `Meter`/`Counter<T>` (named like `app.user.logins`, with unit and
   description) — this keeps metric code out of business handlers. Use the
   shared `"ChurchBulletin.Application"` `Meter` name (or a new one you
   registered in step 2) rather than deriving rates from log/trace volume
   after the fact.
7. **Don't over-tag.** Skip PII (employee email, names) and anything
   unbounded (full request/response bodies, large collections). Tag
   identifiers and small scalars; if you need the full payload for
   debugging, that's a log message with a scope, not a span tag that gets
   held in an exporter's backend indefinitely. `Bus.cs` enforces this at
   the MediatR boundary: `AddPropertyTags` only tags message properties
   explicitly opted in with `[TelemetryTag]` (defined in
   `src/Core/TelemetryTagAttribute.cs`) and truncates values to 128
   characters. A new message type produces no property tags until opted
   in — that safe default is intentional. Annotate identifiers and small
   scalars (e.g. `WorkOrderByNumberQuery.Number`,
   `ApplicationChatQuery.CurrentUsername`); never annotate PII
   (`Employee`-typed properties) or unbounded payloads (chat prompts).
8. **Keep logs correlated, not parallel.** Use `ILogger`/`LogContext`
   scopes so log lines pick up the ambient `Activity`'s trace/span IDs
   automatically; don't add `Console.WriteLine`, a second logging
   framework, or a bespoke file sink for a new component — Serilog already
   fans out to every provider that needs it.
9. **Ask whether this needs a health check.** A new hard external dependency
   (a new downstream API, a new required config) that can be down without
   the app crashing on startup is a candidate for an `IHealthCheck`
   registered in `src/UI/Server/UIServiceRegistry.cs` via
   `AddHealthChecks().AddCheck<T>(...)` — follow
   `CanConnectToLlmServerHealthCheck` as the exemplar. It then surfaces
   automatically at `/_healthcheck` and `/_healthcheck/detailed`. Something
   already covered by DB/SQL client instrumentation usually doesn't need
   one. (`AddDefaultHealthChecks()` in ServiceDefaults only registers the
   trivial `"self"` check — real dependency checks don't go there.) For
   hosts other than UI.Server (Worker, McpServer in HTTP mode), ask the
   same question — a host with no mapped health endpoint has no liveness
   story for its orchestrator at all.

## What "good" looks like vs. what to flag

- Good: a new `WorkOrderNotificationClient` wraps its `HttpClient` calls in
  its own `ActivitySource("ChurchBulletin.Notifications", "1.0.0")`,
  registers it in `Extensions.cs`, tags `notification.channel` and
  `notification.workOrderNumber`, increments a
  `workorders.notifications.sent` counter, and rethrows on failure with
  `ActivityStatusCode.Error` set.
- Flag for follow-up if you see: a raw `new HttpClient()`/`SqlConnection`
  call with no instrumentation at a genuinely new boundary; an `ActivitySource`
  created but never added to `Extensions.cs`; a `catch` block that logs and
  swallows instead of tagging-and-rethrowing; span/log tags containing an
  employee's email or full description text; or a parallel `Console.WriteLine`
  / file-logging path introduced instead of using `ILogger`.

## When this doesn't apply

Pure in-memory domain logic with no I/O, no cross-process call, and no
failure path worth surfacing (e.g. `WorkOrderStatus` smart-enum comparisons,
`CanReassign()`) doesn't need tracing or metrics. Instrumentation earns its
keep at boundaries someone would actually want visibility into during an
incident — don't sprinkle spans on every method for their own sake.
