# C4 Architecture: Container Diagram

Icons: [Tabler](https://icones.js.org/collection/tabler) via [icones.js.org](https://icones.js.org/). [Register icon pack](https://mermaid.js.org/config/icons.html) to render (e.g. `@iconify-json/tabler`, name `tabler`).

```mermaid
C4Container
  title Container diagram

  Person(someuser, "Name", "Description", "tabler:user")

  System_Boundary(system, "Church Bulletin") {
    ContainerDb(db, "Database", "Azure SQL Database", "Detail", "tabler:database")
    Container(appservice, "App Service", "Web or Container", "Detail", "tabler:server")
    Container(ui, "UI/user app", "Blazor WASM", "Detail", "tabler:app-window")
  }

  Rel_R(someuser, ui, "Uses", "http")
  Rel_R(ui, appservice, "Calls", "http")
  Rel_R(appservice, db, "Calls", "tcp")
```
