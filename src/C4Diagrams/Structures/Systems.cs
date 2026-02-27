using C4Sharp.Elements;
using C4Sharp.Elements.Relationships;

namespace C4Diagrams.Structures;

public static class Systems
{
    public static SoftwareSystem ChurchBulletin => new(
        alias: "churchbulletin",
        label: "Church Bulletin",
        description: "Digital signage and printed bulletin"
    );

    public static SoftwareSystem Printer => new(
        alias: "printer",
        label: "Printer",
        description: "",
        boundary: Boundary.External
    );

    public static SoftwareSystem Projector => new(
        alias: "projector",
        label: "Projector",
        description: "",
        boundary: Boundary.External
    );
}
