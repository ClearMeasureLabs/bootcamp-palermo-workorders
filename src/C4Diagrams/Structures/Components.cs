using C4Sharp.Elements;

namespace C4Diagrams.Structures;

public static class Components
{
    public static Component Core => new(
        alias: "core",
        label: "Core",
        technology: "Inner layer of onion architecture",
        description: "netstandard2.1/net60"
    );

    public static Component DataAccess => new(
        alias: "dataAccess",
        label: "DataAccess",
        technology: "House Entity Framework",
        description: "Handle interaction with SQL Server"
    );

    public static Component DatabaseProject => new(
        alias: "databaseProject",
        label: "Database",
        technology: "Manage creation and migrating database schema",
        description: ""
    );

    public static Component UnitTests => new(
        alias: "unitTests",
        label: "Unit Tests",
        technology: "Tests all in-memory logic",
        description: ""
    );

    public static Component IntegrationTests => new(
        alias: "integrationTests",
        label: "Integration Tests",
        technology: "Tests all logic that flows between different memory spaces",
        description: ""
    );

    public static Component UiServer => new(
        alias: "uiServer",
        label: "Api",
        technology: "Blazor server project housing web api endpoints",
        description: ""
    );

    public static Component UiClient => new(
        alias: "uiClient",
        label: "User Interface",
        technology: "Blazor Wasm interactive application",
        description: ""
    );

    public static Component Startup => new(
        alias: "startup",
        label: "App Startup",
        technology: "Bootstraps dependencies and starts application",
        description: ""
    );
}
