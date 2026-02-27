using C4Sharp.Diagrams;
using C4Sharp.Diagrams.Builders;
using C4Sharp.Elements;
using C4Sharp.Elements.Relationships;
using static C4Sharp.Elements.ContainerType;

namespace C4Diagrams.Diagrams;

public class ContainerDeploymentDiagram : ContainerDiagram
{
    protected override string Title => "Container diagram";
    protected override DiagramLayout FlowVisualization => DiagramLayout.LeftRight;

    protected override IEnumerable<Structure> Structures =>
    [
        Person.None | Boundary.External | (
            alias: "someuser",
            label: "Name",
            description: "Description"
        ),

        Bound("system", "Church Bulletin",
            Container.None | (
                type: Database,
                alias: "db",
                label: "Database",
                technology: "Azure SQL Database",
                description: "Detail"
            ),
            Container.None | (
                type: WebApplication,
                alias: "appservice",
                label: "App Service",
                technology: "Web or Container",
                description: "Detail"
            ),
            Container.None | (
                type: Spa,
                alias: "ui",
                label: "UI/user app",
                technology: "Blazor WASM",
                description: "Detail"
            )
        )
    ];

    protected override IEnumerable<Relationship> Relationships => new[]
    {
        this["someuser"] > this["ui"] | ("Uses", "http"),
        this["ui"] > this["appservice"] | ("Calls", "http"),
        this["appservice"] > this["db"] | ("Calls", "tcp")
    };
}
