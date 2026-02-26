# AGENTS.md

See `CLAUDE.md` for full project overview, solution structure, architecture, coding standards, and build/test commands.

## Cursor Cloud specific instructions

### System Dependencies

- **.NET SDK 10.0.100** (prerelease) - installed via `dotnet-install.sh`
- **PowerShell 7 (pwsh)** - required for all build scripts (`build.ps1`, `PrivateBuild.ps1`, `BuildFunctions.ps1`)
- **Docker** - required for SQL Server container on Linux; needs `fuse-overlayfs` storage driver and `iptables-legacy` in the cloud VM

### Running the Full Build

```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File ./PrivateBuild.ps1
```

This runs clean, restore, compile, unit tests, Docker SQL Server setup, DB migration, and integration tests. It auto-detects the database engine (SQL-Container on Linux with Docker).

### Running the Application

The `launchSettings.json` contains a Windows-only LocalDB connection string that crashes on Linux. To run the app on Linux, bypass the launch profile and set environment variables manually:

```bash
export ConnectionStrings__SqlConnectionString="server=localhost,1433;database=ChurchBulletin;User ID=sa;Password=churchbulletin-mssql#1A;TrustServerCertificate=true;"
export ASPNETCORE_ENVIRONMENT=Development
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=586d68ed-85bc-4092-ac8a-fabb7a583e93;IngestionEndpoint=https://centralus-2.in.applicationinsights.azure.com/;LiveEndpoint=https://centralus.livediagnostics.monitor.azure.com/;ApplicationId=5328e763-3c56-4eae-ad66-aa528a92e984"
export AI_OpenAI_ApiKey=""
export AI_OpenAI_Url=""
export AI_OpenAI_Model=""
cd src/UI/Server && dotnet run --no-launch-profile --urls "https://localhost:7174;http://localhost:5174"
```

Key gotchas:
- **Must use `--no-launch-profile`** to avoid the LocalDB connection string override from `launchSettings.json`.
- **Must set `APPLICATIONINSIGHTS_CONNECTION_STRING`** or the Azure Monitor exporter will crash on startup.
- **Must set `AI_OpenAI_*` vars to empty strings** to prevent the app from trying to connect to Azure OpenAI (it degrades gracefully).
- The SQL Server Docker container must already be running (created by `PrivateBuild.ps1` or manually via `docker run`). The container name is `churchbulletin-mssql` and the password is `churchbulletin-mssql#1A`.

### Docker Daemon

In the cloud VM, Docker needs to be started manually:

```bash
sudo dockerd &>/tmp/dockerd.log &
sleep 5
sudo chmod 666 /var/run/docker.sock
```

### Database

The build scripts auto-detect the database engine. On Linux with Docker, SQL Server 2022 runs in a container on port 1433. The container is named `churchbulletin-mssql` with password `churchbulletin-mssql#1A`. The `PrivateBuild.ps1` script handles container creation, database creation, and migration automatically.

### SQLite Fallback

If Docker is unavailable, set `DATABASE_ENGINE=SQLite` before running the build scripts. The app and integration tests will use SQLite via EF Core's `EnsureCreated`. Some integration tests tagged `SqlServerOnly` will be skipped.

### Optional Services

- **Ollama** (localhost:11434): Local LLM for AI agent features. Not required; errors in logs about Ollama connection refused are expected and harmless.
- **Azure OpenAI**: Cloud LLM alternative. Requires `AI_OpenAI_ApiKey`, `AI_OpenAI_Url`, `AI_OpenAI_Model` env vars.

### Gotchas

- NServiceBus runs in trial mode (no license). This produces a warning at startup but does not block functionality.
- The HTTPS dev certificate is untrusted. Browser interactions require clicking through the security warning.
- The `appsettings.Development.json` has a LocalDB connection string; on Linux, always override via the `ConnectionStrings__SqlConnectionString` environment variable or use the build scripts which handle this automatically.
