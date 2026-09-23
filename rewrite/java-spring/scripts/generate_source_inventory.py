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
    parts = path.parts
    area = parts[1] if len(parts) > 1 else "Other"
    relative = path.relative_to(SOURCE_ROOT).as_posix()
    if area == "Core":
        if relative.startswith("Core/Model/"):
            return "domain", "planned; only core lifecycle and urgency currently ported"
        if relative.startswith(("Core/Services/", "Core/Queries/", "Core/Validation/")):
            return "application", "planned; selected work-order behavior currently ported"
        if relative.startswith("Core/Import/"):
            return "application.import", "deferred"
        return "domain/application", "deferred"
    if area == "DataAccess":
        return "persistence", "planned; JPA persistence exists, most handlers are deferred"
    if area == "Database":
        return "src/main/resources/db/migration", "planned; Flyway baseline added, source migration parity deferred"
    if area in {"UnitTests", "IntegrationTests", "AcceptanceTests"}:
        return "src/test/java", "planned; selected domain, API, browser tests ported"
    if area.startswith("UI"):
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
            target, status = destination(source)
            rows.append((relative.as_posix(), source.suffix.lower().lstrip("."), source.parts[1], target, status))
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT.open("w", newline="", encoding="utf-8") as output:
        writer = csv.writer(output)
        writer.writerow(("source_path", "file_type", "area", "intended_java_destination", "port_status"))
        writer.writerows(rows)
    print(f"Wrote {len(rows)} source entries to {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
