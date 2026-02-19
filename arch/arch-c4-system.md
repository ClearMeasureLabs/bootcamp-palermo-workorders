# Church Bulletin System Diagram

Icons use the [Tabler](https://icones.js.org/collection/tabler) pack from [icones.js.org](https://icones.js.org/). To render with icons, [register the icon pack](https://mermaid.js.org/config/icons.html) (e.g. `@iconify-json/tabler`, name `tabler`).

```mermaid
C4Context
  title Church Bulletin System Diagram
  UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")

  Person(pastor, "Senior Pastor", "Any clergy leader", "tabler:user")
  Person(biblestudyleader, "Bible study leader", "leads classes", "tabler:users")
  Person(worshipleader, "Worship Pastor", "music/choir", "tabler:music")
  Person(childrenspastor, "Childrens' Pastor", "Kids ministry", "tabler:brand-apple-arcade")
  Person(volunteer, "Volunteer", "Prepares bulletins and projects announcements", "tabler:hand-stop")

  System(churchbulletin, "Church Bulletin", "Digital signage and printed bulletin", "tabler:news")

  System_Ext(printer, "Printer", "", "tabler:printer")
  System_Ext(projector, "Projector", "", "tabler:device-tv")

  Rel_R(pastor, churchbulletin, "Add sermons")
  Rel_R(biblestudyleader, churchbulletin, "Add classes")
  Rel_R(worshipleader, churchbulletin, "Add services")
  Rel_R(childrenspastor, churchbulletin, "Add sunday school classes")
  Rel_R(volunteer, churchbulletin, "Operates system on Sunday morning")

  Rel_R(churchbulletin, printer, "Send PDF to print", "Network printer")
  Rel_R(churchbulletin, projector, "Projects digital signage", "Auto-animated")
```
