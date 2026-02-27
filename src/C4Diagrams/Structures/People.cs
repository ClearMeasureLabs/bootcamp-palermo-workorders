using C4Sharp.Elements;
using C4Sharp.Elements.Relationships;

namespace C4Diagrams.Structures;

public static class People
{
    public static Person Pastor => new(
        alias: "pastor",
        label: "Senior Pastor",
        description: "Any clergy leader",
        boundary: Boundary.External
    );

    public static Person BibleStudyLeader => new(
        alias: "biblestudyleader",
        label: "Bible study leader",
        description: "Leads classes",
        boundary: Boundary.External
    );

    public static Person WorshipLeader => new(
        alias: "worshipleader",
        label: "Worship Pastor",
        description: "Music/choir",
        boundary: Boundary.External
    );

    public static Person ChildrensPastor => new(
        alias: "childrenspastor",
        label: "Childrens' Pastor",
        description: "Kids ministry",
        boundary: Boundary.External
    );

    public static Person Volunteer => new(
        alias: "volunteer",
        label: "Volunteer",
        description: "Prepares bulletins and projects announcements",
        boundary: Boundary.External
    );

    public static Person SomeUser => new(
        alias: "someuser",
        label: "Name",
        description: "Description",
        boundary: Boundary.External
    );
}
