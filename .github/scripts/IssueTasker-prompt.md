Generate technical development tasks for this GitHub issue.

ARCHITECTURE CONTEXT:
- .NET 9.0 Onion Architecture: Core (domain) -> DataAccess (EF Core) -> UI (Blazor WASM)
- Domain: WorkOrder, Employee, WorkOrderStatus, Role in src/Core/
- Data: EF Core handlers in src/DataAccess/, MediatR CQRS pattern
- UI: Blazor in src/UI/Server/ and src/UI/Client/
- DB: AliaSQL migrations in src/Database/scripts/Update/ (numbered ###_Name.sql)
- Tests: NUnit + Shouldly in src/UnitTests/, src/IntegrationTests/

RULES:
- Tasks must reference specific file paths or layers
- Include unit test tasks for new functionality
- Follow Onion Architecture (no Core dependencies on outer layers)

OUTPUT FORMAT: One task per line, no bullets or numbers, no explanations.

ISSUE: {title}

{body}
