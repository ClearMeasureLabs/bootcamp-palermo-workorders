## 1. Project Setup

- [ ] 1.1 Create `src/McpServer/` console project targeting .NET 10.0 and add it to `ChurchBulletin.sln`
- [ ] 1.2 Add project references to Core and DataAccess
- [ ] 1.3 Add `ModelContextProtocol` NuGet package for MCP server hosting
- [ ] 1.4 Add `Lamar` and `MediatR` NuGet packages for DI and handler registration
- [ ] 1.5 Add EF Core SQL Server NuGet package for DataContext registration

## 2. Host and DI Configuration

- [ ] 2.1 Create `Program.cs` with `Host.CreateDefaultBuilder` and `.AddMcpServer().WithStdioTransport()`
- [ ] 2.2 Create `McpServiceRegistry.cs` (Lamar ServiceRegistry) registering MediatR handlers, IBus, and DataContext — mirroring UIServiceRegistry patterns
- [ ] 2.3 Add `appsettings.json` with database connection string configuration
- [ ] 2.4 Register MCP tools and resources in the host builder

## 3. Work Order Tools

- [ ] 3.1 Implement `list-work-orders` tool using `WorkOrderSpecificationQuery` via IBus
- [ ] 3.2 Implement `get-work-order` tool using `WorkOrderByNumberQuery` via IBus
- [ ] 3.3 Implement `create-work-order` tool using `SaveDraftCommand` via IBus
- [ ] 3.4 Implement `execute-work-order-command` tool that resolves the named IStateCommand and sends it via IBus
- [ ] 3.5 Implement `update-work-order-description` tool using `UpdateDescriptionCommand` via IBus

## 4. Employee Tools

- [ ] 4.1 Implement `list-employees` tool using `EmployeeGetAllQuery` via IBus
- [ ] 4.2 Implement `get-employee` tool using `EmployeeByUserNameQuery` via IBus

## 5. Reference Resources

- [ ] 5.1 Implement `churchbulletin://reference/work-order-statuses` resource returning all WorkOrderStatus values
- [ ] 5.2 Implement `churchbulletin://reference/roles` resource returning all Role values
- [ ] 5.3 Implement `churchbulletin://reference/status-transitions` resource returning the valid state transition map

## 6. Integration Tests

- [ ] 6.1 Add MCP server integration test project or test class in existing IntegrationTests
- [ ] 6.2 Write tests for each work order tool (list, get, create, execute command, update description)
- [ ] 6.3 Write tests for each employee tool (list, get)
- [ ] 6.4 Write tests for each reference resource (statuses, roles, transitions)

## 7. Documentation and Configuration

- [ ] 7.1 Add MCP server configuration entry for Claude Code (`.claude/mcp.json` or similar) so the server can be launched locally
- [ ] 7.2 Verify the solution builds end-to-end with `dotnet build src/ChurchBulletin.sln`
