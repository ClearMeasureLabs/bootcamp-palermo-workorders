# Architecture diagrams (Mermaid flowcharts and class diagrams)

Diagrams are in Mermaid format (`.mmd`). View with VS Code Mermaid extension, GitHub (when embedded in Markdown), or [Mermaid Live Editor](https://mermaid.live/).

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
