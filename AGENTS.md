# AGENTS.md

## Cursor Cloud specific instructions

### Prerequisites

The VM comes with .NET 10.0 SDK, PowerShell 7, and Docker pre-installed. The update script handles NuGet restore; no additional system dependencies are needed.

### Database (SQL Server in Docker)

The build scripts (`build.ps1`) auto-detect Linux and use Docker-based SQL Server (`mcr.microsoft.com/mssql/server:2022-latest`) with container name `churchbulletin-mssql`. The password convention is `{container-name}#1A` (e.g., `churchbulletin-mssql#1A`).

Before running integration tests or the app, start Docker and set up the database:

```bash
# Start Docker daemon (if not running)
sudo dockerd &>/dev/null &

# Start SQL Server container
docker rm -f churchbulletin-mssql 2>/dev/null
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=churchbulletin-mssql#1A" \
  -p 1433:1433 --name churchbulletin-mssql -d mcr.microsoft.com/mssql/server:2022-latest

# Wait for readiness (~5-10s), then create and migrate DB
docker exec -e "SQLCMDPASSWORD=churchbulletin-mssql#1A" churchbulletin-mssql \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -d master -Q "CREATE DATABASE [ChurchBulletin];" -C

dotnet run --project src/Database --configuration Release -- rebuild "localhost,1433" "ChurchBulletin" "src/Database/scripts" "sa" "churchbulletin-mssql#1A"
```

Set the connection string env var for the app and integration tests:

```bash
export ConnectionStrings__SqlConnectionString="server=localhost,1433;database=ChurchBulletin;User ID=sa;Password=churchbulletin-mssql#1A;TrustServerCertificate=true;"
```

### Running commands

Refer to `CLAUDE.md` for build, test, and run commands. Key differences for cloud agents:

- **Unit tests** do not require a database: `dotnet test src/UnitTests --configuration Release`
- **Integration tests** require the SQL Server container and `ConnectionStrings__SqlConnectionString` env var.
- **UI.Server** runs on `https://localhost:7174` (HTTP on `http://localhost:5174`). The dev certificate is untrusted; use `-k` with curl.
- The **PrivateBuild** script (`./build.sh` or `pwsh PrivateBuild.ps1`) handles Docker setup automatically on Linux if Docker is running.

### SQLite fallback

If Docker is unavailable, set `DATABASE_ENGINE=SQLite` before running the build scripts. The app and integration tests will use SQLite via EF Core's `EnsureCreated`. Some integration tests tagged `SqlServerOnly` will be skipped.

### Gotchas

- NServiceBus runs in trial mode (no license). This produces a warning at startup but does not block functionality.
- The HTTPS dev certificate is untrusted. Browser interactions require clicking through the security warning.
- The `appsettings.Development.json` has a LocalDB connection string; on Linux, always override via the `ConnectionStrings__SqlConnectionString` environment variable.
