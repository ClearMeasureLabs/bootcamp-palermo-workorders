#!/usr/bin/env python3
"""Regenerate the file-by-file inventory of first-party .NET source files."""

import csv
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = ROOT / "src"
OUTPUT = ROOT / "rewrite/java-spring/SOURCE_INVENTORY.csv"
SOURCE_EXTENSIONS = {
    ".cs", ".razor", ".sql", ".proto", ".cshtml", ".js", ".css", ".html"
}


def destination(path: Path) -> tuple[str, str]:
    relative = path.relative_to(SOURCE_ROOT).as_posix()
    area = relative.split("/", 1)[0] if "/" in relative else "Other"
    if area == "Core":
        if relative in {"Core/Model/Employee.cs", "Core/Model/Role.cs"}:
            return "domain", "employee and role capability model ported; other source behavior deferred"
        if relative.startswith("Core/Model/StateCommands/"):
            return "domain/application", "partial; creator/assignee actor checks and listed lifecycle actions ported"
        if relative.startswith("Core/Model/"):
            return "domain", "planned; only core lifecycle and urgency currently ported"
        if relative.startswith("Core/Queries/Employee"):
            return "application", "employee selection/read ported; remaining employee operations deferred"
        if relative.startswith(("Core/Services/", "Core/Queries/", "Core/Validation/")):
            return "application", "planned; selected work-order behavior currently ported"
        if relative.startswith("Core/Import/"):
            return "application.import", "deferred"
        return "domain/application", "deferred"
    if area == "DataAccess":
        if relative.startswith("DataAccess/Mappings/EmployeeMap.cs") or relative.startswith("DataAccess/Mappings/RoleMap.cs"):
            return "persistence", "employee/role and capability mappings ported with Flyway V3"
        if relative.startswith("DataAccess/Handlers/EmployeeQueryHandler.cs"):
            return "persistence", "employee login selection/query partially ported"
        if relative.startswith("DataAccess/Handlers/StateCommandHandler"):
            return "persistence/application", "actor ownership checks ported for selected lifecycle actions"
        return "persistence", "planned; JPA persistence exists, most handlers are deferred"
    if area == "Database":
        return "src/main/resources/db/migration", "planned; Flyway baseline added, source migration parity deferred"
    if area in {"UnitTests", "IntegrationTests", "AcceptanceTests"}:
        if relative.startswith(("AcceptanceTests/Authentication/", "UnitTests/UI.Shared/Pages/Login", "UnitTests/UI.Shared/Components/Logout")):
            return "src/test/java", "login/session/logout behavior partially ported; extra source cases deferred"
        if relative.startswith(("UnitTests/Core/Model/StateCommands/", "IntegrationTests/DataAccess/Handlers/StateCommandHandler")):
            return "src/test/java", "ownership, role, lifecycle authorization cases partially ported"
        return "src/test/java", "planned; selected domain, API, browser tests ported"
    if area.startswith("UI"):
        if relative.startswith(("UI.Shared/Pages/Login", "UI.Shared/Components/Logout", "UI.Shared/Authentication/")):
            return "web/auth", "username-only demo login, Lovejoy shortcut, logout and server session ported"
        if relative.startswith("UI.Shared/NavMenu.razor"):
            return "web/ui", "role-based create affordance and current employee menu ported"
        return "web/ui", "planned; initial MVC/API slice ported, remaining UI behavior deferred"
    if area == "Worker":
        return "worker", "deferred"
    if area == "McpServer":
        return "mcp", "deferred"
    if area == "LlmGateway":
        return "ai", "deferred"
    if area in {"ChurchBulletin.AppHost", "ChurchBulletin.ServiceDefaults"}:
        return "deployment/observability", "deferred"
    return "review", "deferred"


def main() -> None:
    rows = []
    for source in sorted(SOURCE_ROOT.rglob("*")):
        if source.is_file() and source.suffix.lower() in SOURCE_EXTENSIONS:
            relative = source.relative_to(ROOT)
            area = source.relative_to(SOURCE_ROOT).parts[0]
            target, status = destination(source)
            rows.append((relative.as_posix(), source.suffix.lower().lstrip("."), area, target, status))
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT.open("w", newline="", encoding="utf-8") as output:
        writer = csv.writer(output, lineterminator="\n")
        writer.writerow(("source_path", "file_type", "area", "intended_java_destination", "port_status"))
        writer.writerows(rows)
    print(f"Wrote {len(rows)} source entries to {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
