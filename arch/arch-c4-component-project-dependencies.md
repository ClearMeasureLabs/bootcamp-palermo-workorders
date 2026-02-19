# C4 Architecture: Component Diagram

Icons: [Tabler](https://icones.js.org/collection/tabler) via [icones.js.org](https://icones.js.org/). [Register icon pack](https://mermaid.js.org/config/icons.html) to render (e.g. `@iconify-json/tabler`, name `tabler`).

```mermaid
C4Component
  title Church Bulletin Component diagram

  ContainerDb(database, "Database", "SQL Database", "Transactional data store", "tabler:database")

  Container_Boundary(visualstudiosolution, "ChurchBulletin.sln") {
    Component(core, "Core", "Inner layer of onion architecture", "netstandard2.1/net60", "tabler:package")
    Component(dataAccess, "DataAccess", "House Entity Framework", "Handle interaction with SQL Server", "tabler:database")
    Component(databaseProject, "Database", "Manage creation and migrating database schema", "AliaSQL", "tabler:schema")
    Component(unitTests, "Unit Tests", "Tests all in-memory logic", "NUnit", "tabler:test-pipe")
    Component(integrationTests, "Integration Tests", "Tests all logic that flows between different memory spaces", "NUnit", "tabler:test-pipe")
    Component(uiServer, "Api", "Blazor server project housing web api endpoints", "ASP.NET", "tabler:server")
    Component(uiClient, "User Interface", "Blazor Wasm interactive application", "Blazor", "tabler:app-window")
    Component(startup, "App Startup", "Bootstraps dependencies and starts application", "Lamar", "tabler:rocket")
  }

  
  Rel(uiServer, core, "Project Reference")
  Rel(uiClient, core, "Project Reference")
  Rel(databaseProject, database, "AliaSQL/DbUP")
  Rel(uiServer, uiClient, "Project Reference")
  Rel(dataAccess, database, "ConnectionString")
  Rel(startup, core, "Project Reference")
  Rel(startup, dataAccess, "Project Reference")
  Rel(startup, uiServer, "Project Reference")
  Rel(startup, uiClient, "Project Reference")
  Rel(dataAccess, core, "Project Reference")
```
