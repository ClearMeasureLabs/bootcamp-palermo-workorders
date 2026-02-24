## 1. Project Setup

- [x] 1.1 Create `src/McpServer/` console project targeting .NET 10.0 and add it to `ChurchBulletin.sln`
- [x] 1.2 Add project references to Core and DataAccess
- [x] 1.3 Add `ModelContextProtocol` NuGet package for MCP server hosting
- [x] 1.4 Add `Lamar` and `MediatR` NuGet packages for DI and handler registration
- [x] 1.5 Add EF Core SQL Server NuGet package for DataContext registration

## 2. Host and DI Configuration

- [x] 2.1 Create `Program.cs` with `Host.CreateDefaultBuilder` and `.AddMcpServer().WithStdioTransport()`
- [x] 2.2 Create `McpServiceRegistry.cs` (Lamar ServiceRegistry) registering MediatR handlers, IBus, and DataContext — mirroring UIServiceRegistry patterns
- [x] 2.3 Add `appsettings.json` with database connection string configuration
- [x] 2.4 Register MCP tools and resources in the host builder

## 3. Work Order Tools

- [x] 3.1 Implement `list-work-orders` tool using `WorkOrderSpecificationQuery` via IBus
- [x] 3.2 Implement `get-work-order` tool using `WorkOrderByNumberQuery` via IBus
- [x] 3.3 Implement `create-work-order` tool using `SaveDraftCommand` via IBus
- [x] 3.4 Implement `execute-work-order-command` tool that resolves the named IStateCommand and sends it via IBus
- [x] 3.5 Implement `update-work-order-description` tool using `UpdateDescriptionCommand` via IBus

## 4. Employee Tools

- [x] 4.1 Implement `list-employees` tool using `EmployeeGetAllQuery` via IBus
- [x] 4.2 Implement `get-employee` tool using `EmployeeByUserNameQuery` via IBus

## 5. Reference Resources

- [x] 5.1 Implement `churchbulletin://reference/work-order-statuses` resource returning all WorkOrderStatus values
- [x] 5.2 Implement `churchbulletin://reference/roles` resource returning all Role values
- [x] 5.3 Implement `churchbulletin://reference/status-transitions` resource returning the valid state transition map

## 6. Integration Tests

- [x] 6.1 Add MCP server integration test project or test class in existing IntegrationTests
- [x] 6.2 Write tests for each work order tool (list, get, create, execute command, update description)
- [x] 6.3 Write tests for each employee tool (list, get)
- [x] 6.4 Write tests for each reference resource (statuses, roles, transitions)

## 7. Documentation and Configuration

- [x] 7.1 Add MCP server configuration entry for Claude Code (`.claude/mcp.json` or similar) so the server can be launched locally
- [x] 7.2 Verify the solution builds end-to-end with `dotnet build src/ChurchBulletin.sln`
