# AGENTS.md

See `CLAUDE.md` for full project overview, solution structure, architecture, coding standards, and build/test commands.

## Cursor Cloud specific instructions

### System Dependencies

- **.NET SDK 10.0.100** (prerelease) - installed via `dotnet-install.sh`
- **PowerShell 7 (pwsh)** - required for all build scripts (`build.ps1`, `PrivateBuild.ps1`, `BuildFunctions.ps1`)
- **Docker** - required for SQL Server container on Linux; needs `fuse-overlayfs` storage driver and `iptables-legacy` in the cloud VM

### Running the Full Build

```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File ./PrivateBuild.ps1
```

This runs clean, restore, compile, unit tests, Docker SQL Server setup, DB migration, and integration tests. It auto-detects the database engine (SQL-Container on Linux with Docker).

### Server logging

UI.Server, Worker, and standalone McpServer emit **structured logs as JSON lines** on the standard console (Serilog `RenderedCompactJsonFormatter`), suitable for container log aggregation (for example Azure Container Apps). Levels and namespace overrides are driven by the `Serilog` section in each host’s `appsettings*.json`.

### Running the Application

The `launchSettings.json` contains a Windows-only LocalDB connection string that crashes on Linux. To run the app on Linux, bypass the launch profile and set environment variables manually:

```bash
export ConnectionStrings__SqlConnectionString="server=localhost,1433;database=ChurchBulletin;User ID=sa;Password=churchbulletin-mssql#1A;TrustServerCertificate=true;"
export ASPNETCORE_ENVIRONMENT=Development
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=586d68ed-85bc-4092-ac8a-fabb7a583e93;IngestionEndpoint=https://centralus-2.in.applicationinsights.azure.com/;LiveEndpoint=https://centralus.livediagnostics.monitor.azure.com/;ApplicationId=5328e763-3c56-4eae-ad66-aa528a92e984"
export AI_OpenAI_ApiKey=""
export AI_OpenAI_Url=""
export AI_OpenAI_Model=""
cd src/UI/Server && dotnet run --no-launch-profile --urls "https://localhost:7174;http://localhost:5174"
```

Key gotchas:
- **Must use `--no-launch-profile`** to avoid the LocalDB connection string override from `launchSettings.json`.
- **Must set `APPLICATIONINSIGHTS_CONNECTION_STRING`** or the Azure Monitor exporter will crash on startup.
- **Must set `AI_OpenAI_*` vars to empty strings** to prevent the app from trying to connect to Azure OpenAI (it degrades gracefully).
- The SQL Server Docker container must already be running (created by `PrivateBuild.ps1` or manually via `docker run`). The container name is `churchbulletin-mssql` and the password is `churchbulletin-mssql#1A`.

### gRPC (work orders)

- **Contract:** `src/UI/Server/Protos/workorders.proto`. Generated C# is checked in under `src/UI/Server/Generated/Protos/` so Linux ARM64 CI avoids `Grpc.Tools` `protoc` (which can segfault on that platform). After changing the `.proto`, regenerate on an x64 machine with `Grpc.Tools` and replace those files.
- **Endpoint:** Same base URL as UI.Server (for example `https://localhost:7174`). Clients should use **HTTP/2** (TLS in development; configure ingress/proxy for HTTP/2 or h2c in production as appropriate).
- **Surface:** `workorders.WorkOrders` — `Ping` (smoke) and `GetWorkOrderByNumber` (reads via `IBus` / `WorkOrderByNumberQuery`, same path as HTTP APIs).
- **Auth:** Anonymous for the shipped RPCs (no `[Authorize]` on the service).
- **Rate limiting:** The sliding-window policy applies only to `/api/*` (and the Blazor single-API path); gRPC calls are not covered by that limiter.
- **Client example (.NET):**

```csharp
using Grpc.Net.Client;
using ClearMeasure.Bootcamp.UI.Server.Grpc;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
using var channel = GrpcChannel.ForAddress("https://localhost:7174");
var client = new WorkOrders.WorkOrdersClient(channel);
var reply = await client.GetWorkOrderByNumberAsync(
    new GetWorkOrderByNumberRequest { Number = "WO-123" });
```

### Docker Daemon

In the cloud VM, Docker needs to be started manually:

```bash
sudo dockerd &>/tmp/dockerd.log &
sleep 5
sudo chmod 666 /var/run/docker.sock
```

### Database

The build scripts auto-detect the database engine. On Linux with Docker, SQL Server 2022 runs in a container on port 1433. The container is named `churchbulletin-mssql` with password `churchbulletin-mssql#1A`. The `PrivateBuild.ps1` script handles container creation, database creation, and migration automatically.

### SQLite Fallback

If Docker is unavailable, set `DATABASE_ENGINE=SQLite` before running the build scripts. The app and integration tests will use SQLite via EF Core's `EnsureCreated`. Some integration tests tagged `SqlServerOnly` will be skipped.

### Optional Services

- **Ollama** (localhost:11434): Local LLM for AI agent features. Not required; errors in logs about Ollama connection refused are expected and harmless.
- **Azure OpenAI**: Cloud LLM alternative. Requires `AI_OpenAI_ApiKey`, `AI_OpenAI_Url`, `AI_OpenAI_Model` env vars.

### GitHub issue updates from scripts

When appending issue bodies via `python3` or other subprocesses: **export** any variable the child reads (`export VAR=...`), or embed the text in the script. Unexported shell variables appear **empty** in the child, which can produce a successful API response with **blank** content. See **GitHub REST API — Issue body updates** in `.cursor/rules/cloud-agent-instructions.mdc`.

### Quick dotnet build / test (without full PrivateBuild.ps1)

When a full PowerShell build is unnecessary (e.g. for a pre-built or docs-only feature), run the targeted sequence:

```bash
cd /workspace/src && dotnet restore ChurchBulletin.sln   # always restore first — obj/project.assets.json may be missing
dotnet build UI/Api/UI.Api.csproj -warnaserror
dotnet test UnitTests/UnitTests.csproj --no-build --filter "FullyQualifiedName~<ClassName>"
```

The solution file is at `src/ChurchBulletin.sln`. Run `dotnet restore` from `/workspace/src` with the `.sln` path; running it from `/workspace` fails because no `.sln` is at the root.

### "Feature already implemented" work items

When the work item body says the feature is **already fully implemented**, verify with:

```bash
find /workspace/src -name "<ExpectedFile>.cs" 2>/dev/null
```

If all listed files exist and compile cleanly, the only remaining tasks are quality gates, self-tuning, merge, and PR — no code to write.

### Gotchas

- NServiceBus runs in trial mode (no license). This produces a warning at startup but does not block functionality.
- The HTTPS dev certificate is untrusted. Browser interactions require clicking through the security warning.
- The `appsettings.Development.json` has a LocalDB connection string; on Linux, always override via the `ConnectionStrings__SqlConnectionString` environment variable or use the build scripts which handle this automatically.

### "Feature Already Implemented" Work Items

When a work item's Technical Design section says **"The feature is fully implemented"** and lists checked-off files, verify those files exist (`find /workspace/src -name "FileName.cs"`), confirm they match the spec, run `dotnet build src/ChurchBulletin.sln --configuration Release -warnaserror` (0 warnings required) and `dotnet test src/UnitTests --filter "FullyQualifiedName~ClassName"`, then proceed directly to the self-tuning/commit/PR steps. Do NOT re-create files that already exist and match the spec — doing so wastes tokens and risks introducing divergence.

### API-Layer-Only Features (Pure Utility Endpoints)

For endpoints in `src/UI/Api/Controllers/` that have no Core/Domain/DataAccess changes:
- Mirror the dual `[Route]` pattern: `"api/tools/<name>"` + `$"{ApiRoutes.VersionedApiPrefix}/tools/<name>"` (see `ToolsGuidGeneratorController`, `ToolsHashController`, `ToolsRandomController`).
- Use `[ApiVersion("1.0")]`, `[AllowAnonymous]`, `[EnableRateLimiting(ApiRateLimiting.PolicyName)]`.
- Return plain text via `ContentResult` with `ContentType = "text/plain; charset=utf-8"`.
- Return structured errors via `Problem(detail: "...", statusCode: 400)`.
- No DI needed for stateless generators — use `Random.Shared` (thread-safe) and static helpers.
- Build verification: `dotnet build src/ChurchBulletin.sln --configuration Release -warnaserror` — 0 warnings required.

### Qodana Static Analysis — Known Conventions

Qodana runs in CI with `failThreshold: 0`. Common P2 findings to pre-empt:

**InconsistentNaming (`non_field_members_should_use_upper_camel_case`)**
- Abbreviations in PascalCase method names must capitalize only the first letter: `EnUs` not `EnUS`, `Id` not `ID` (unless the framework demands it), etc.
- Affects test method names too — rename consistently across all test files.

**Nullability (`ReturnTypeCanBeNotNullable`, `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract`)**
- If a private/helper method always returns a non-null value, declare its return type as non-nullable (`T` not `T?`).
- Update the corresponding local variable declarations to match.

**Visibility / MemberCanBePrivate (`MemberCanBePrivate.Global`, `MemberCanBeProtected.Global`)**
- Before narrowing any property or method, run these **four** checks, in order:
  1. **Blazor `[Parameter]`** — any property decorated with `[Parameter]` or `[SupplyParameterFromQuery]` must stay `public`.  Razor templates in the same partial class can access `private` members fine; only cross-component parameters need `public`.
  2. **JSON serialization** — `System.Text.Json` (used by `WebServiceMessage` remoting) cannot populate `private set` properties during deserialization. Any property on a class that participates in the remoting round-trip (`IRemotableRequest`) must keep `public set`. Verify with the `ShouldSerialize` / `AssertRemotable` unit test in `RemotableRequestTests`.
  3. **Cross-assembly callers** — `grep -r "MemberName" src/` before narrowing. A `public static` test-helper called from `IntegrationTests` or other test projects must stay `public`; a helper only used within the same class can be `private`.
  4. **Object-initializer setters** — `new SomeType { Property = value }` syntax requires at minimum `internal set` (or `public set`); `private set` breaks this. Search for `{ Property =` across the solution before narrowing.
- For members that cannot be narrowed due to the above constraints, add a single-line suppression: `// ReSharper disable once MemberCanBePrivate.Global -- <reason>`.
- **Private field naming**: When narrowing a `public` field to `private`, rename it to `_camelCase` (underscore prefix) to satisfy `InconsistentNaming`. Update ALL references in both the `.cs` file and the `.razor` template (they are the same partial class and share the name).
- **Private auto-property setters**: When narrowing visibility, if the setter is never assigned after construction, change `{ get; set; }` to `{ get; }` to avoid `AutoPropertyCanBeMadeGetOnly.Local`. If the property was previously `protected virtual` (`.Global` in baseline), also remove that baseline entry — it becomes absent and fails the gate.
- **`virtual` keyword**: Removing `virtual` from a `protected virtual` member is often required when narrowing to `private` — `private virtual` is illegal and `private` overrides are invisible to subclasses.
- **After all visibility changes**: Update `qodana.sarif.json` baseline to remove entries for every finding that is now fixed or suppressed. Absent findings (in baseline but not in scan) count against `failThreshold: 0` just like new findings. Remove them using a Python script that filters by `partialFingerprints.equalIndicator/v1`. Keep only findings that are genuinely still present in the code (e.g., cross-assembly public helpers that cannot be narrowed).
- After all changes: `dotnet build src/ChurchBulletin.sln --configuration Release -warnaserror` must pass with 0 warnings, followed by the full `UnitTests` run.

After any rename that touches test methods, verify no callers outside the file reference the old name (use `grep -r "OldName" src/`).
