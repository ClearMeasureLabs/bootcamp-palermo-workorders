Generate development tasks for this GitHub issue.

ARCHITECTURE CONTEXT:
- .NET 9.0 Onion Architecture: Core (domain) -> DataAccess (EF Core) -> UI (Blazor WASM)
- Domain: WorkOrder, Employee, WorkOrderStatus, Role in src/Core/
- Data: EF Core handlers in src/DataAccess/, MediatR CQRS pattern
- UI: Blazor in src/UI/Server/ and src/UI/Client/
- DB: AliaSQL migrations in src/Database/scripts/ (numbered ###_Name.sql)
- Tests: NUnit + Shouldly in src/UnitTests/, src/IntegrationTests/

RULES:
- Write tasks at a human-readable level describing WHAT to accomplish, not HOW
- Do NOT include specific file paths, class names, or method names
- Do NOT mention implementation details like "EF Core mapping", "HasMaxLength", "ALTER TABLE"
- Tasks should describe the functional change, not the code change
- Example good task: "Update the database to support longer work order titles"
- Example bad task: "Update EF Core mapping for Title field to HasMaxLength 650 in src/DataAccess/Mappings/WorkOrderMap.cs"
- Include verification tasks (tests) but describe what to verify, not how

OUTPUT FORMAT: One task per line, no bullets or numbers, no explanations.

ISSUE: {title}

{body}
