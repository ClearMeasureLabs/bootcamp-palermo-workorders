using C4Sharp.Diagrams.Builders;
using C4Sharp.Elements;
using C4Sharp.Elements.Relationships;

namespace C4Diagrams.Diagrams;

public class AssignedToInProgressCommandSequence : SequenceDiagram
{
    protected override string Title => "AssignedToInProgressCommand Sequence";

    protected override IEnumerable<Structure> Structures =>
    [
        new Person(alias: "user", label: "User", description: ""),
        new Component(
            alias: "api",
            label: "SingleApiController",
            type: ComponentType.None,
            technology: "UI.Server",
            description: "API endpoint"
        ),
        new Component(
            alias: "bus",
            label: "Bus : IBus",
            type: ComponentType.None,
            technology: "UI.Server",
            description: "Message bus"
        ),
        new Component(
            alias: "mediator",
            label: "IMediator",
            type: ComponentType.None,
            technology: "MediatR",
            description: ""
        ),
        Bound(alias: "cmdBound", label: "Command Pipeline",
            new Component(
                alias: "cmd",
                label: "AssignedToInProgressCommand",
                type: ComponentType.None,
                technology: "Command",
                description: ""
            ),
            new Component(
                alias: "cmdHandler",
                label: "StateCommandHandler",
                type: ComponentType.None,
                technology: "Handler",
                description: ""
            ),
            new Component(
                alias: "order",
                label: "WorkOrder",
                type: ComponentType.None,
                technology: "Domain",
                description: ""
            )
        ),
        new Container(
            alias: "db",
            label: "DataContext / SQL Server",
            type: ContainerType.Database,
            technology: "EF Core",
            description: ""
        )
    ];

    protected override IEnumerable<Relationship> Relationships =>
    [
        It("user") > It("api") | "User input",
        It("api") > It("bus") | "Send(AssignedToInProgressCommand)",
        It("bus") > It("mediator") | "Send(AssignedToInProgressCommand)",
        It("mediator") > It("cmdHandler") | "Handle(StateCommandBase)",
        It("cmdHandler") > It("cmd") | "Execute(StateCommandContext)",
        It("cmd") > It("order") | "ChangeStatus(CurrentUser, date, InProgress)",
        It("cmdHandler") > It("db") | "SaveChangesAsync()",
        It("cmdHandler") > It("mediator") | ("StateCommandResult", "Begin")
    ];
}
