# Lab 01: Environment Setup & First Contribution

**Curriculum Section:** Warm-up
**Estimated Time:** 30 minutes
**Type:** Build / Hands-on

---

## Objective

Confirm the development toolchain works end-to-end, make a first code change through the full pull request workflow, and become familiar with the build pipeline.

---

## Prerequisites

- Developer-grade Windows 11 x64 computer with full administrator account
- Unrestricted GitHub account
- Visual Studio 2026 with .NET 10.0 SDK
- JetBrains ReSharper with "IntelliJ IDEA" keymap selected
- SQL Server LocalDB (or better)

---

## Steps

### Step 1: Clone the Repository

```powershell
git clone https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders
cd bootcamp-palermo-workorders
```

### Step 2: Run the Private Build

```powershell
.\privatebuild.ps1
```

This executes the full local build: compile, unit tests, database migration, and integration tests. Verify all tests pass with a green result.

> **If the build fails:** Check that SQL Server LocalDB is installed and running. The build auto-detects the database engine (LocalDB on Windows, Docker SQL on Linux, SQLite fallback).

### Step 3: Explore the Build Script

Open `build.ps1` in your editor. Identify these key functions:

| Function | Purpose |
|----------|---------|
| `Init` | Clean and restore NuGet packages |
| `Compile` | Build solution with warnings-as-errors |
| `UnitTests` | Run unit tests with code coverage |
| `IntegrationTest` | Run integration tests against a real database |
| `Package-Everything` | Create deployment packages |

**Question to answer:** What is the database engine detection logic? (Hint: look for `Setup-DatabaseForBuild`)

### Step 4: Open the Solution

Open `src/ChurchBulletin.sln` in Visual Studio. Explore the Solution Explorer:

- **Core** — Domain models, interfaces, queries (zero project references)
- **DataAccess** — EF Core context, MediatR handlers (references Core only)
- **UI/Server, UI/Client, UI.Shared, UI/Api** — Blazor WebAssembly + Server UI
- **Database** — DbUp migration scripts
- **UnitTests, IntegrationTests, AcceptanceTests** — Test projects

### Step 5: Add Yourself to ZDataLoader

Open `src/IntegrationTests/ZDataLoader.cs`. Find the placeholder comments (`//Person 1` through `//Person 13`).

Add yourself as a new Employee following the `jpalermo` pattern (lines 24-28):

```csharp
//Person 1
var yourName = new Employee("yourusername", "YourFirst", "YourLast", "you@email.com");
yourName.AddRole(lead);
yourName.AddRole(fulfillment);
db.Add(yourName);
```

### Step 6: Verify the Build Still Passes

```powershell
.\privatebuild.ps1
```

### Step 7: Run the Application

```powershell
cd src/UI/Server
dotnet run
```

Navigate to `https://localhost:7174`. Verify your name appears in the login dropdown. Check health at `https://localhost:7174/_healthcheck`.

### Step 8: Submit a Pull Request

```powershell
git checkout -b yourusername/add-myself-to-dataloader
git add src/IntegrationTests/ZDataLoader.cs
git commit -m "Add [YourName] to ZDataLoader"
git push -u origin yourusername/add-myself-to-dataloader
```

Create a pull request on GitHub using the template in `.github/pull_request_template.md`.

---

## Expected Outcome

- Green build on `privatebuild.ps1`
- Your name visible in the application login dropdown
- An open pull request on GitHub

---

## Discussion Questions

1. What are the different stages of the build pipeline?
2. Why does the build run both unit tests AND integration tests locally?
3. What role does the ZDataLoader play in the development workflow?
