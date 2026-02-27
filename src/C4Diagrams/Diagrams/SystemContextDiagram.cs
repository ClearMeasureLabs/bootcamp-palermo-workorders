using C4Sharp.Diagrams.Builders;
using C4Sharp.Elements;
using C4Sharp.Elements.Relationships;
using static C4Diagrams.Structures.People;
using static C4Diagrams.Structures.Systems;

namespace C4Diagrams.Diagrams;

public class SystemContextDiagram : ContextDiagram
{
    protected override string Title => "Church Bulletin System Diagram";

    protected override IEnumerable<Structure> Structures =>
    [
        Pastor,
        BibleStudyLeader,
        WorshipLeader,
        ChildrensPastor,
        Volunteer,
        ChurchBulletin,
        Printer,
        Projector
    ];

    protected override IEnumerable<Relationship> Relationships =>
    [
        Pastor > ChurchBulletin | "Add sermons",
        BibleStudyLeader > ChurchBulletin | "Add classes",
        WorshipLeader > ChurchBulletin | "Add services",
        ChildrensPastor > ChurchBulletin | "Add sunday school classes",
        Volunteer > ChurchBulletin | "Operates system on Sunday morning",
        ChurchBulletin > Printer | ("Send PDF to print", "Network printer"),
        ChurchBulletin > Projector | ("Projects digital signage", "Auto-animated")
    ];
}
