using C4Sharp.Elements;
using static C4Sharp.Elements.ContainerType;

namespace C4Diagrams.Structures;

public static class Containers
{
    public static Container Db => Container.None | (
        type: Database,
        alias: "db",
        label: "Database",
        technology: "Azure SQL Database",
        description: "Detail"
    );

    public static Container AppService => Container.None | (
        type: WebApplication,
        alias: "appservice",
        label: "App Service",
        technology: "Web or Container",
        description: "Detail"
    );

    public static Container Ui => Container.None | (
        type: Spa,
        alias: "ui",
        label: "UI/user app",
        technology: "Blazor WASM",
        description: "Detail"
    );
}
