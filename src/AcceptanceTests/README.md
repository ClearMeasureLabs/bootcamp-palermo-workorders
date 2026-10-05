# Acceptance tests

Playwright and NUnit tests of the whole application in a browser. The Build workflow runs the full suite (jobs
"Acceptance Tests" and "Acceptance Tests (ARM SQLite)"), and the Deploy workflow runs it against TDD after each
deployment ("Run Acceptance tests"). Against a deployed environment the suite runs in remote mode
(`StartLocalServer=false`): tests that need a local server or the Worker skip themselves. Tests that call the AI model
are `[Explicit]` and run only when selected by name.

## Smoke tests

The tests marked `[Category("Smoke")]` are the smoke set: the few tests that together show that a deployment's
connections work. Every screen a user can reach from the app's links opens at least once, and each integration in the
table below is touched once; the list after the table names what the set does not cover. A post-deployment run can
select only this set instead of the full suite (with the suite's run settings, which cap the parallel Playwright
workers):

```bash
dotnet test src/AcceptanceTests --configuration Release --settings src/AcceptanceTests/AcceptanceTests.runsettings \
  --filter "TestCategory=Smoke"
```

| Screen or integration | Smoke test | What it proves after a deployment |
|---|---|---|
| Home (signed out), Login, signing in | `LoginTests.LoginWithUsernameOnlyForwardsToHomePage` | The header's Login link opens the login screen; its employee list comes from the database through the API; signing in as a seeded user (`hsimpson`) posts the login event to the server and lands on Home |
| Home (signed in), database read | `WorkOrderStatusDashboardTests.ShouldShowStatusCardsWhenAuthenticated` | The status cards show a count per status from a database query through the API |
| New work order, Search, Edit work order, database write | `WorkOrderSaveDraftTests.ShouldCreateNewWorkOrderAndVerifyOnSearchScreen` | A draft saved from the New work order screen is stored (the test reads it back from the database), listed on Search, and opens on the edit screen with the saved values |
| Settings | `DarkModeTests.DarkMode_ShouldToggleHtmlDataTheme_WhenSwitchChangedOnSettings` | Settings opens from the menu, the theme switch works, and Search opens from the menu |
| AI Agent | `NavMenuTests.AiAgent_NavLink_ShouldOpenAiAgentPage` | The AI Agent screen opens from the menu; it needs no chat client, unlike the chat tests |
| Health status screen, health | `ClientHealthCheckTests.Should_NavigateToHealthCheck_WhenGearIconClicked` | The footer's health link opens `/_clienthealthcheck`, whose checks call the server's health check and the API from the browser. Before any test, the suite's health gate (`ServerFixture`) requires `/_healthcheck` to answer Healthy or Degraded |
| Not found, deep links | `CopyrightFooterTests.ShouldShowCopyrightFooter_OnNotFoundRoute` | A hard navigation to an unknown path gets the app, which shows its not-found screen |
| MCP endpoint | `McpHttpServerAcceptanceTests.ShouldListWorkOrdersViaHttp` | `/mcp` answers an MCP client and lists work orders from the database |

The API every screen uses (`/api/blazor-wasm-single-api`) and the Blazor WebAssembly download are exercised by every
browser test above.

Not covered after a deployment:

- **Message bus** (NServiceBus on the SQL Server transport). The only user action that sends a message is assigning a
  work order to the AI bot (`aibot`, `WorkOrderAssignedToBotEvent`); no acceptance test does that, and its effect
  needs the Worker, which `TracerBulletTests` also needs (it skips without it). A deployment proves the transport only
  indirectly: UI.Server starts its endpoint on the transport, so a broken one stops the app and fails the health gate.
- **REST API** (`/api/...` controllers): the `Api/` tests skip themselves against a deployed app.
- **gRPC** (`WorkOrdersGrpcService`) and the WebSocket notifications (`/ws/notifications`): no acceptance tests.
- **Chat client** (AI Agent and work order chat, the MCP tests with an LLM): those tests are `[Explicit]`.
- **Telemetry export**: not visible to a browser.
- Pages with no link in the app: `/counter` (`CounterTests`, full suite only), `/fetchdata` and
  `/_clienthealthcheck/detailed` (no test opens them).

Keep the set small. A new screen or a new connection gets one smoke test, the cheapest existing test that proves it;
change this table in the same pull request.
