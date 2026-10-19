# Environments

- **TDD** — the continuous integration environment built on every commit to master.
- **UAT** — the pre-production environment used for validation before release.
- **Production** — the live environment serving real users.

## Local (SQLite, no Docker)

Use this path on Linux/macOS when Docker is not available. Set `DATABASE_ENGINE=SQLite` so the build and tests use SQLite instead of SQL Server.

The default connection string is `Data Source=ChurchBulletin.db`, so the database file is created as `ChurchBulletin.db` in the process working directory (typically `src/` when running the build scripts).

Build (compile, unit tests, migration/seed, integration tests):

```bash
export DATABASE_ENGINE=SQLite
cd src
pwsh -NoProfile -ExecutionPolicy Bypass -File ./PrivateBuild.ps1
```

Integration tests only:

```bash
export DATABASE_ENGINE=SQLite
dotnet test src/IntegrationTests --configuration Release
```

To run the app (not just the build), also clear the SQL connection string, seed reference data, then start the UI — see README.md "SQLite fallback (no Docker)" and "Run locally with SQLite".
