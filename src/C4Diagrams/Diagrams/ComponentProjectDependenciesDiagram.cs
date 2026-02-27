using C4Sharp.Diagrams.Builders;
using C4Sharp.Elements;
using C4Sharp.Elements.Relationships;
using static C4Sharp.Elements.ContainerType;
using static C4Diagrams.Structures.Components;

namespace C4Diagrams.Diagrams;

public class ComponentProjectDependenciesDiagram : ComponentDiagram
{
    protected override string Title => "Church Bulletin Component diagram";

    protected override IEnumerable<Structure> Structures =>
    [
        Container.None | (
            type: Database,
            alias: "database",
            label: "Database",
            technology: "SQL Database",
            description: "Transactional data store"
        ),

        Bound("visualstudiosolution", "ChurchBulletin.sln",
            Core,
            DataAccess,
            DatabaseProject,
            UnitTests,
            IntegrationTests,
            UiServer,
            UiClient,
            Startup
        )
    ];

    protected override IEnumerable<Relationship> Relationships =>
    [
        DataAccess > Core | "Project Reference",
        UiServer > Core | "Project Reference",
        UiClient > Core | "Project Reference",
        DatabaseProject > this["database"] | "DbUp",
        UiServer > UiClient | "Project Reference",
        DataAccess > this["database"] | "ConnectionString",
        Startup > Core | "Project Reference",
        Startup > DataAccess | "Project Reference",
        Startup > UiServer | "Project Reference",
        Startup > UiClient | "Project Reference"
    ];
}
