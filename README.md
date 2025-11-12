# Work Order Management System

A comprehensive work order management system built with .NET 9.0, implementing Onion Architecture principles with a Blazor WebAssembly UI. This system enables organizations to create, assign, track, and complete maintenance work orders with integrated AI assistance.

## Features

- **Work Order Management**: Create, edit, assign, and track work orders through their complete lifecycle
- **Status Workflow**: Draft → Assigned → In Progress → Complete → Cancelled/Shelved
- **Employee Management**: User authentication and role-based access control
- **AI Assistant Integration**: Azure OpenAI and Ollama integration for intelligent work order assistance
- **Real-time Updates**: Blazor WebAssembly provides responsive, real-time UI updates
- **Comprehensive Testing**: Unit, integration, and end-to-end acceptance tests with Playwright

## Technology Stack

### Backend
- **.NET 9.0**: Latest .NET framework
- **Entity Framework Core 9.0**: Data access with SQL Server
- **MediatR**: CQRS pattern implementation
- **Lamar**: Dependency injection container (StructureMap successor)
- **AliaSQL**: Database migration tool

### Frontend
- **Blazor WebAssembly**: Modern single-page application framework
- **Blazor Server**: Server-side rendering support
- **bUnit**: Blazor component testing

### Database
- **SQL Server**: Production database
- **LocalDB**: Development database
- **AliaSQL**: Version-controlled database migrations

### AI/LLM Integration
- **Azure OpenAI**: Cloud-based AI assistance
- **Ollama**: Local LLM support

### Testing
- **NUnit 4.x**: Testing framework
- **Playwright**: End-to-end browser automation
- **Shouldly**: Fluent assertion library
- **AutoBogus**: Test data generation

### Deployment
- **Azure Container Apps**: Containerized deployment
- **Docker**: Container packaging
- **Azure DevOps**: CI/CD pipelines
- **Octopus Deploy**: Release management

## Architecture

This project implements **Onion Architecture** with strict dependency rules ensuring the core domain logic remains independent of infrastructure concerns.

### Project Structure

```
src/
├── Core/                           # Domain layer (no dependencies)
│   ├── Model/                     # Domain entities (WorkOrder, Employee, etc.)
│   ├── Queries/                   # CQRS query objects
│   └── Services/                  # Domain service interfaces
├── DataAccess/                    # Data access layer (depends on Core only)
│   ├── Mappings/                  # EF Core entity mappings
│   └── Handlers/                  # MediatR query/command handlers
├── Database/                      # AliaSQL migration scripts
│   └── scripts/
│       └── Update/                # Numbered migration scripts (001, 003, 004...)
├── UI/
│   ├── Client/                    # Blazor WebAssembly client
│   ├── Server/                    # Blazor Server hosting
│   ├── Shared/                    # Shared UI components
│   └── Api/                       # Web API endpoints
├── LlmGateway/                    # AI integration layer
├── UnitTests/                     # Unit tests
├── IntegrationTests/              # Integration tests
└── AcceptanceTests/               # Playwright end-to-end tests
```

### Dependency Flow

```
UI → DataAccess → Core
         ↓
    LlmGateway → Core
```

**Key Principles**:
- Core has no dependencies on other projects
- DataAccess only references Core
- UI layers reference both Core and DataAccess
- Domain logic resides in Core
- Infrastructure concerns stay in outer layers

### CQRS Pattern

The application uses **MediatR** to implement Command Query Responsibility Segregation:
- **Queries**: Read operations returning data (e.g., `WorkOrderByNumberQuery`, `EmployeeByUserNameQuery`)
- **Commands**: Write operations via `IStateCommand` interface
- **Handlers**: Distributed across DataAccess and UI layers based on responsibility

## Getting Started

### Prerequisites

- **.NET 9.0 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **SQL Server LocalDB**: Included with Visual Studio or [download standalone](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- **PowerShell 7+**: For build scripts
- **Node.js** (optional): For Playwright browser automation

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd bootcamp-palermo-workorders-claude-code-cli
   ```

2. **Build the solution**
   ```powershell
   # Quick build
   .\build.bat

   # Full private build (recommended for first-time setup)
   .\build.ps1
   PrivateBuild
   ```

3. **Database setup**

   The PrivateBuild process automatically:
   - Creates a unique LocalDB database
   - Runs all AliaSQL migration scripts
   - Seeds test data

   To manually migrate the database:
   ```powershell
   $databaseServer = "(LocalDb)\MSSQLLocalDB"
   $databaseName = "WorkOrderSystem"
   MigrateDatabaseLocal -databaseServerFunc $databaseServer -databaseNameFunc $databaseName
   ```

4. **Run the application**
   ```bash
   cd src/UI/Server
   dotnet run
   ```

   Application will be available at:
   - **HTTPS**: https://localhost:7174
   - **Health Check**: https://localhost:7174/_healthcheck

### Build Commands

#### Quick Build
```powershell
.\build.bat
```
Compiles the solution in Debug mode.

#### Private Build
```powershell
.\build.ps1
PrivateBuild
```
Complete local development build including:
1. Clean solution
2. Compile all projects
3. Run unit tests (with code coverage)
4. Create temporary database
5. Run database migrations
6. Run integration tests
7. Database cleanup

#### CI Build
```powershell
.\build.ps1
CIBuild
```
Includes PrivateBuild plus:
- Package creation
- Artifact publishing to Azure Artifacts

### Running Tests

#### Unit Tests
```powershell
cd src/UnitTests
dotnet test --configuration Release
```
- **92 tests** covering domain logic, validation, and business rules
- Uses **Shouldly** for assertions
- Follows AAA pattern (Arrange, Act, Assert)

#### Integration Tests
```powershell
cd src/IntegrationTests
dotnet test --configuration Release
```
- **41 tests** verifying data access, EF Core mappings, and database operations
- Uses LocalDB with automatic database creation/cleanup
- Tests include: entity persistence, query handlers, database constraints

#### Acceptance Tests
```powershell
cd src/AcceptanceTests

# Install Playwright browsers (first time only)
pwsh bin/Debug/net9.0/playwright.ps1 install

# Run tests
dotnet test --configuration Debug
```
- End-to-end tests using Playwright browser automation
- Tests cover: work order creation, assignment, status changes, AI chat integration
- Runs against actual Blazor WebAssembly application

### Database Migrations

This project uses **AliaSQL** for version-controlled database migrations.

#### Migration Script Location
```
src/Database/scripts/Update/
```

#### Creating New Migrations

1. Create a new numbered SQL script in `src/Database/scripts/Update/`:
   ```
   ###_Description.sql
   ```
   Example: `023_AddPriorityFieldToWorkOrder.sql`

2. Use the next sequential number (current highest is 022)

3. Write your migration SQL:
   ```sql
   -- Add Priority field to WorkOrder table
   ALTER TABLE [dbo].[WorkOrder]
       ADD [Priority] NVARCHAR(50) NULL;
   GO
   ```

4. Run PrivateBuild to apply locally:
   ```powershell
   .\build.ps1
   PrivateBuild
   ```

#### Migration Actions
- **Create**: Create new database
- **Update**: Apply incremental migrations
- **Rebuild**: Drop and recreate (used in PrivateBuild)
- **TestData**: Load test data

## Domain Model

### Core Entities

#### WorkOrder
Primary entity representing a maintenance work order.

**Properties**:
- `Number`: Unique 7-character identifier (e.g., "WO-0001")
- `Title`: Short description (max 200 chars)
- `Description`: Detailed description (max 4000 chars)
- `Instructions`: Optional execution instructions (max 4000 chars)
- `RoomNumber`: Location identifier (max 50 chars)
- `Status`: Current workflow state
- `Creator`: Employee who created the work order
- `Assignee`: Employee assigned to complete the work
- `CreatedDate`, `AssignedDate`, `CompletedDate`: Timestamps

#### WorkOrderStatus
Enum defining the work order lifecycle:
- **Draft**: Initial state, editable
- **Assigned**: Assigned to an employee
- **InProgress**: Work has started
- **Complete**: Work finished successfully
- **Cancelled**: Work order cancelled
- **Shelved**: Work order postponed

#### Employee
Represents system users.

**Properties**:
- `UserName`: Unique identifier
- `FirstName`, `LastName`: Name components
- `EmailAddress`: Contact information
- `Roles`: Collection of assigned roles

#### Role
Defines user permissions:
- **Creator**: Can create work orders
- **Assignee**: Can be assigned work orders

### Business Rules

1. **Work orders can only be reassigned in Draft status**
   ```csharp
   public bool CanReassign() => Status == WorkOrderStatus.Draft;
   ```

2. **Description and Instructions auto-truncate at 4000 characters**
   - Prevents data loss from overly long inputs
   - Enforced at domain level

3. **State transitions follow defined workflow**
   - Managed through `IStateCommand` implementations
   - Validated by `StateCommandList`

## Coding Standards

### Naming Conventions
- **PascalCase**: Classes, methods, properties, public members
- **camelCase**: Local variables, private fields, parameters
- **Prefix test doubles with "Stub"**: Not "Mock"

### Testing Standards
- **Use Shouldly** for all test assertions
- **Follow AAA pattern** without explicit comments
- **Test method naming**: `[MethodName]_[Scenario]_[ExpectedResult]` or start with "Should"/"When"
- **Small, focused methods**: Single responsibility principle

### Architecture Rules
- **Core project**: No external dependencies (pure domain logic)
- **DataAccess project**: Only references Core
- **Do not add NuGet packages** without approval
- **Maintain .NET SDK versions** unless specifically instructed to upgrade

## Contributing

### Workflow

1. **Branch naming**: `feature/{date}-{issue-number}-{ai-model}-{ide}-{description}`
   - Example: `feature/20251112-120000-issue50-claude-sonnet-4.5-claudecodecli-workorders`

2. **Development process**:
   - Checkout master and pull latest changes
   - Create feature branch
   - Implement changes following architecture rules
   - Run PrivateBuild locally
   - Commit with descriptive messages
   - Push and create pull request

3. **Pull request requirements**:
   - All tests must pass (unit, integration, acceptance)
   - Follow existing code style and patterns
   - Include test coverage for new features
   - Update documentation if needed

### Commit Message Format
```
Brief summary (50 chars or less)

Detailed explanation of changes including:
- What was changed and why
- Technical details
- Breaking changes (if any)

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

## CI/CD Pipeline

The project uses Azure DevOps pipelines defined in `src/pure-azdo-pipeline.yml`.

### Pipeline Stages

1. **Integration_Build**
   - Build solution
   - Run unit and integration tests
   - Package application
   - Push to Azure Artifacts

2. **Docker Build & Push**
   - Build Docker container
   - Push to Azure Container Registry

3. **TDD Environment**
   - Auto-deploy to test environment
   - Run database migrations
   - Execute acceptance tests
   - Destroy environment resources

4. **UAT Environment**
   - Manual approval required
   - Deploy to staging
   - Database migration
   - Manual testing

5. **PROD Environment**
   - Manual approval required
   - Production deployment
   - Database migration
   - Health monitoring

### Versioning
Format: `{major}.{minor}.{Rev:r}`
- Current version: 1.3.x
- Automatically incremented by build pipeline

## Infrastructure & Deployment

For detailed information about Azure infrastructure setup, Octopus Deploy configuration, and production deployment, see [DEPLOYMENT.md](DEPLOYMENT.md).

### Quick Deploy Overview

**Requirements**:
- Azure subscription
- Azure Container Registry
- Octopus Deploy instance
- Azure DevOps project
- GitHub repository

**Environments**:
- **TDD**: Temporary test environment (auto-created/destroyed)
- **UAT**: User acceptance testing (persistent)
- **PROD**: Production (persistent)

**Container Deployment**:
- Application runs in Azure Container Apps
- Database uses Azure SQL
- Infrastructure created programmatically via Azure CLI scripts
- Auto-scaling configured via Octopus runbooks

## Troubleshooting

### Build Issues

**Problem**: Build fails with SDK version mismatch
```
Solution: Ensure .NET 9.0 SDK is installed
dotnet --list-sdks
```

**Problem**: Database connection fails
```
Solution: Verify LocalDB is running
SqlLocalDB info MSSQLLocalDB
SqlLocalDB start MSSQLLocalDB
```

### Test Issues

**Problem**: Acceptance tests fail to start browser
```
Solution: Install Playwright browsers
pwsh src/AcceptanceTests/bin/Debug/net9.0/playwright.ps1 install
```

**Problem**: Integration tests fail with database errors
```
Solution: Ensure LocalDB is accessible and rebuild database
.\build.ps1
PrivateBuild
```

## Architecture Documentation

PlantUML diagrams are available in the `arch/` directory:
- `arch-c4-system.puml`: System context diagram
- `arch-c4-container-deployment.puml`: Container deployment view
- `arch-c4-component-project-dependencies.puml`: Component dependencies
- `arch-c4-class-domain-model.puml`: Domain model (WorkOrder, Employee, Status, Role)

## Additional Resources

### Documentation
- [CLAUDE.md](CLAUDE.md): AI assistant instructions for working with this codebase
- [Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/): Original article by Jeffrey Palermo
- [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)

### Related Projects
- [AliaSQL](https://github.com/ClearMeasure/AliaSQL): Database migration tool
- [Lamar](https://jasperfx.github.io/lamar/): Dependency injection container

## License

This project is part of the Clear Measure Bootcamp training program.

## Support

For questions or issues:
- Create an issue in the GitHub repository
- Contact the development team
- Review the CLAUDE.md file for AI-assisted development guidance
