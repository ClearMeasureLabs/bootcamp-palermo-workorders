# Architecture diagrams (Mermaid C4 and flowcharts)

Diagrams are in Mermaid format (`.mmd`). View with VS Code Mermaid extension, GitHub (when embedded in Markdown), or [Mermaid Live Editor](https://mermaid.live/).

## C4 system

| File | Description |
|------|-------------|
| `c4-system-context.mmd` | System context: users, Church Bulletin system, Azure SQL, LLM, printer, Octopus |

## C4 container (per process head)

| File | Description |
|------|-------------|
| `c4-container-ui-server.mmd` | UI.Server: Blazor Server, Web API, AI agents, DB, NServiceBus |
| `c4-container-worker.mmd` | Worker: endpoints, handlers, sagas, DB, LlmGateway |
| `c4-container-integration-tests.mmd` | IntegrationTests: TestHost, IntegratedTestBase, DB tests, handler tests |
| `c4-container-acceptance-tests.mmd` | AcceptanceTests: ServerFixture, Playwright, app/workorder/AI tests |
| `c4-container-unittests.mmd` | UnitTests: Core, UI, Server test groups |
| `c4-container-apphost.mmd` | ChurchBulletin.AppHost: Aspire host, UI.Server, Worker |

## C4 component (per .NET project)

| File | Description |
|------|-------------|
| `c4-component-core.mmd` | Core: Model, Queries, Services, StateCommands, Events |
| `c4-component-dataaccess.mmd` | DataAccess: Mappings, Handlers, Messaging, Health |
| `c4-component-ui-server.mmd` | UI.Server: Program, UIServiceRegistry, Controllers, Agents, Health |
| `c4-component-ui-client.mmd` | UI.Client: Program, UIClientServiceRegistry, RemotableBus, Health |
| `c4-component-ui-api.mmd` | UI.Api: Controllers, HealthCheck, Pages |
| `c4-component-ui-shared.mmd` | UI.Shared: Pages, Models, Authentication, Bus |
| `c4-component-llmgateway.mmd` | LlmGateway: WorkOrderChatHandler, WorkOrderTool, ChatClientFactory, Health |
| `c4-component-worker.mmd` | Worker: Program, WorkOrderEndpoint, Handlers, Sagas, Messaging |
| `c4-component-database.mmd` | Database: Console, Commands, DatabaseOptions |
| `c4-component-service-defaults.mmd` | ChurchBulletin.ServiceDefaults: Extensions, telemetry |
| `c4-component-unittests.mmd` | UnitTests: Core, UI.Shared, UI.Server, UI.Client tests |
| `c4-component-integration-tests.mmd` | IntegrationTests: IntegratedTestBase, TestHost, DB, handlers |
| `c4-component-acceptance-tests.mmd` | AcceptanceTests: AcceptanceTestBase, ServerFixture, App/WorkOrders/AI tests |

## Class diagrams (per namespace)

| File | Description |
|------|-------------|
| `class-core-model.mmd` | Core.Model: EntityBase, WorkOrder, Employee, Role, WorkOrderStatus |
| `class-core-statecommands.mmd` | Core.Model.StateCommands: IStateCommand, commands, StateCommandResult |
| `class-core-queries-services.mmd` | Core.Queries / Core.Services: IBus, queries, interfaces |
| `class-dataaccess.mmd` | DataAccess: DataContext, mappings, handlers, DistributedBus |
| `class-worker.mmd` | Worker: WorkOrderEndpoint, Handlers, Sagas, Events, RemotableBus |

## Flowcharts

| File | Description |
|------|-------------|
| `flowchart-build-yml.mmd` | .github/workflows/build.yml: jobs and steps |
| `flowchart-deploy-yml.mmd` | .github/workflows/deploy.yml: TDD → UAT → Prod |
| `flowchart-build-ps1.mmd` | build.ps1 / BuildFunctions.ps1: Init, Compile, tests, package, Invoke-* |
| `flowchart-acceptancetests-ps1.mmd` | AcceptanceTests.ps1: params, dot source build.ps1, Invoke-AcceptanceTests |
