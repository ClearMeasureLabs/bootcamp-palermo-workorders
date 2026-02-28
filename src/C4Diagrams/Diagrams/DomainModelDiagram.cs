using C4Sharp.Diagrams.Builders;
using C4Sharp.Elements;
using C4Sharp.Elements.Relationships;

namespace C4Diagrams.Diagrams;

public class DomainModelDiagram : ComponentDiagram
{
    protected override string Title => "Work Order System domain model";

    protected override IEnumerable<Structure> Structures =>
    [
        new Component(
            alias: "entityBase",
            label: "EntityBase<T>",
            technology: "Base class",
            description: "Id and equality behavior"
        ),
        new Component(
            alias: "listItem",
            label: "ListItem",
            technology: "Base class",
            description: "Display/value list abstraction"
        ),
        new Component(
            alias: "workOrder",
            label: "WorkOrder",
            technology: "Domain entity",
            description: "Aggregate root for work requests"
        ),
        new Component(
            alias: "employee",
            label: "Employee",
            technology: "Domain entity",
            description: "User profile and role membership"
        ),
        new Component(
            alias: "role",
            label: "Role",
            technology: "Domain entity",
            description: "Authorization role"
        ),
        new Component(
            alias: "workOrderStatus",
            label: "WorkOrderStatus",
            technology: "Value object",
            description: "Status code/key/friendly-name"
        )
    ];

    protected override IEnumerable<Relationship> Relationships =>
    [
        It("workOrder") > It("entityBase") | "inherits",
        It("employee") > It("entityBase") | "inherits",
        It("role") > It("entityBase") | "inherits",
        It("workOrder") > It("workOrderStatus") | ("status", "1..1 composition"),
        It("workOrder") > It("employee") | ("creator", "0..1 association"),
        It("workOrder") > It("employee") | ("assignee", "0..1 association"),
        It("employee") > It("role") | ("roles", "0..* composition")
    ];
}
