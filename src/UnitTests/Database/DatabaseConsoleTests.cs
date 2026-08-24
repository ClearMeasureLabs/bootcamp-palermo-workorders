using ClearMeasure.Bootcamp.Database.Console;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Database;

[TestFixture]
public class DatabaseConnectionStringBuilderTests
{
    [Test]
    public void Should_BuildIntegratedSecurity_ForLocalhostWithoutCredentials()
    {
        var options = new DatabaseOptions
        {
            DatabaseServer = "localhost",
            DatabaseName = "ChurchBulletin"
        };

        var connectionString = DatabaseConnectionStringBuilder.Build(options);
        var builder = new SqlConnectionStringBuilder(connectionString);

        builder.DataSource.ShouldBe("localhost,1433");
        builder.InitialCatalog.ShouldBe("ChurchBulletin");
        builder.IntegratedSecurity.ShouldBeTrue();
        builder.Encrypt.ShouldBe(SqlConnectionEncryptOption.Optional);
        builder.TrustServerCertificate.ShouldBeTrue();
    }

    [Test]
    public void Should_BuildSqlAuthentication_ForRemoteServerWithCredentials()
    {
        var options = new DatabaseOptions
        {
            DatabaseServer = "sql.example.com",
            DatabaseName = "ChurchBulletin",
            DatabaseUser = "sa",
            DatabasePassword = "secret"
        };

        var connectionString = DatabaseConnectionStringBuilder.Build(options);
        var builder = new SqlConnectionStringBuilder(connectionString);

        builder.DataSource.ShouldBe("sql.example.com,1433");
        builder.IntegratedSecurity.ShouldBeFalse();
        builder.UserID.ShouldBe("sa");
        builder.Password.ShouldBe("secret");
        builder.Encrypt.ShouldBe(SqlConnectionEncryptOption.Mandatory);
        builder.TrustServerCertificate.ShouldBeFalse();
    }

    [Test]
    public void Should_PreserveLocalDbDataSource_WithoutAddingPort()
    {
        var options = new DatabaseOptions
        {
            DatabaseServer = @"(localdb)\MSSQLLocalDB",
            DatabaseName = "ChurchBulletin"
        };

        var connectionString = DatabaseConnectionStringBuilder.Build(options);
        var builder = new SqlConnectionStringBuilder(connectionString);

        builder.DataSource.ShouldBe(@"(localdb)\MSSQLLocalDB");
    }

    [Test]
    public void Should_PreserveExplicitPort_WhenAlreadySpecified()
    {
        var options = new DatabaseOptions
        {
            DatabaseServer = "db.example.com,1434",
            DatabaseName = "ChurchBulletin"
        };

        var connectionString = DatabaseConnectionStringBuilder.Build(options);
        var builder = new SqlConnectionStringBuilder(connectionString);

        builder.DataSource.ShouldBe("db.example.com,1434");
    }

    [Test]
    public void Should_Throw_WhenUserProvidedWithoutPassword()
    {
        var options = new DatabaseOptions
        {
            DatabaseServer = "localhost",
            DatabaseName = "ChurchBulletin",
            DatabaseUser = "sa"
        };

        Should.Throw<ArgumentException>(() => DatabaseConnectionStringBuilder.Build(options))
            .ParamName.ShouldBe(nameof(DatabaseOptions.DatabasePassword));
    }

    [TestCase("localhost", true)]
    [TestCase("127.0.0.1", true)]
    [TestCase(@"(localdb)\MSSQLLocalDB", true)]
    [TestCase("sql.example.com", false)]
    public void Should_ClassifyLocalServer(string serverName, bool expectedLocal)
    {
        DatabaseConnectionStringBuilder.IsLocalServer(serverName).ShouldBe(expectedLocal);
    }

    [TestCase(@"MyServer\LocalDbInstance", true)]
    [TestCase("sql.example.com", false)]
    public void Should_ClassifyLocalDb(string serverName, bool expectedLocalDb)
    {
        DatabaseConnectionStringBuilder.IsLocalDb(serverName).ShouldBe(expectedLocalDb);
    }

    [TestCase("server.example.com", false, "server.example.com,1433")]
    [TestCase(@"server\instance", true, @"server\instance")]
    [TestCase("server,1434", true, "server,1434")]
    public void Should_FormatDataSource(string dataSource, bool isLocalDb, string expected)
    {
        DatabaseConnectionStringBuilder.FormatDataSource(dataSource, isLocalDb).ShouldBe(expected);
    }
}

[TestFixture]
public class DatabaseOptionsTests
{
    [Test]
    public void Should_FailValidation_WhenDatabaseServerMissing()
    {
        var options = new DatabaseOptions { DatabaseName = "ChurchBulletin" };

        var result = options.Validate();

        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("Database server is required");
    }

    [Test]
    public void Should_FailValidation_WhenDatabaseNameMissing()
    {
        var options = new DatabaseOptions { DatabaseServer = "localhost" };

        var result = options.Validate();

        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("Database name is required");
    }

    [Test]
    public void Should_FailValidation_WhenUsernameWithoutPassword()
    {
        var options = new DatabaseOptions
        {
            DatabaseServer = "localhost",
            DatabaseName = "ChurchBulletin",
            DatabaseUser = "sa"
        };

        var result = options.Validate();

        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("Database password is required when username is provided");
    }

    [Test]
    public void Should_FailValidation_WhenPasswordWithoutUsername()
    {
        var options = new DatabaseOptions
        {
            DatabaseServer = "localhost",
            DatabaseName = "ChurchBulletin",
            DatabasePassword = "secret"
        };

        var result = options.Validate();

        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("Database username is required when password is provided");
    }

    [Test]
    public void Should_PassValidation_WhenRequiredFieldsProvided()
    {
        var options = new DatabaseOptions
        {
            DatabaseServer = "localhost",
            DatabaseName = "ChurchBulletin"
        };

        options.Validate().Successful.ShouldBeTrue();
    }

    [Test]
    public void Should_PassValidation_WhenBothCredentialsProvided()
    {
        var options = new DatabaseOptions
        {
            DatabaseServer = "localhost",
            DatabaseName = "ChurchBulletin",
            DatabaseUser = "sa",
            DatabasePassword = "secret"
        };

        options.Validate().Successful.ShouldBeTrue();
    }
}

[TestFixture]
public class ScriptBaselineMarkerTests
{
    [Test]
    public void Should_ReturnZero_WhenScriptDirectoryMissing()
    {
        var result = ScriptBaselineMarker.MarkScriptsInDirectory(
            "Server=localhost;Database=test;Integrated Security=true",
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            "Create");

        result.ShouldBe(0);
    }
}

[TestFixture]
public class DatabaseRebuildStepsTests
{
    [Test]
    public void Should_ReturnFailureResult_WithMessage()
    {
        var result = DatabaseResult.Failure("upgrade failed");

        result.Successful.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("upgrade failed");
    }

    [Test]
    public void Should_ReturnSuccessResult()
    {
        DatabaseResult.Success().Successful.ShouldBeTrue();
    }

    [Test]
    public void RunFullRebuild_WhenConnectionUnreachable_ReturnsFailure()
    {
        var scriptRoot = Path.Combine(Path.GetTempPath(), "crap-rebuild-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(scriptRoot, "Create"));
        Directory.CreateDirectory(Path.Combine(scriptRoot, "Update"));
        Directory.CreateDirectory(Path.Combine(scriptRoot, "Everytime"));
        Directory.CreateDirectory(Path.Combine(scriptRoot, "TestData"));
        try
        {
            var connectionString =
                "Server=127.0.0.1,1;Database=ChurchBulletinCrapGate;User ID=sa;Password=invalid;" +
                "TrustServerCertificate=true;Encrypt=false;Connect Timeout=1;";

            var result = DatabaseRebuildSteps.RunFullRebuild(connectionString, scriptRoot, new NullUpgradeLog());

            result.Successful.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        }
        finally
        {
            Directory.Delete(scriptRoot, recursive: true);
        }
    }
}

[TestFixture]
public class BaselineDatabaseCommandTests
{
    [Test]
    public void MarkCreateAndUpdateScripts_WhenDirectoriesMissing_ReturnsZero()
    {
        var scriptRoot = Path.Combine(Path.GetTempPath(), "crap-baseline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scriptRoot);
        try
        {
            var result = BaselineDatabaseCommand.MarkCreateAndUpdateScripts(
                "Server=localhost;Database=test;Integrated Security=true;TrustServerCertificate=true",
                scriptRoot);

            result.ShouldBe(0);
        }
        finally
        {
            Directory.Delete(scriptRoot, recursive: true);
        }
    }

    [Test]
    public void ExecuteInternal_WhenScriptDirsMissing_ReturnsZero()
    {
        var scriptRoot = Path.Combine(Path.GetTempPath(), "crap-baseline-exec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scriptRoot);
        try
        {
            var cmd = new BaselineDatabaseCommand();
            var method = typeof(BaselineDatabaseCommand).GetMethod(
                "ExecuteInternal",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method.ShouldNotBeNull();

            var options = new DatabaseOptions
            {
                ScriptDir = scriptRoot,
                DatabaseName = "CrapGateBaseline"
            };

            var result = (int)method.Invoke(
                cmd,
                [null!, options, "Server=localhost;Database=test;Integrated Security=true;TrustServerCertificate=true", CancellationToken.None])!;

            result.ShouldBe(0);
        }
        finally
        {
            Directory.Delete(scriptRoot, recursive: true);
        }
    }
}

[TestFixture]
public class RebuildDatabaseCommandTests
{
    [Test]
    public void ExecuteInternal_WhenConnectionUnreachable_ReturnsFailureCode()
    {
        var scriptRoot = Path.Combine(Path.GetTempPath(), "crap-rebuild-exec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(scriptRoot, "Create"));
        Directory.CreateDirectory(Path.Combine(scriptRoot, "Update"));
        Directory.CreateDirectory(Path.Combine(scriptRoot, "Everytime"));
        Directory.CreateDirectory(Path.Combine(scriptRoot, "TestData"));
        try
        {
            var cmd = new RebuildDatabaseCommand();
            var method = typeof(RebuildDatabaseCommand).GetMethod(
                "ExecuteInternal",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method.ShouldNotBeNull();

            var options = new DatabaseOptions
            {
                ScriptDir = scriptRoot,
                DatabaseName = "CrapGateRebuild",
                SuppressUpgradeConsoleOutput = true
            };
            var connectionString =
                "Server=127.0.0.1,1;Database=ChurchBulletinCrapGate;User ID=sa;Password=invalid;" +
                "TrustServerCertificate=true;Encrypt=false;Connect Timeout=1;";

            var result = (int)method.Invoke(
                cmd,
                [null!, options, connectionString, CancellationToken.None])!;

            result.ShouldBe(-1);
        }
        finally
        {
            Directory.Delete(scriptRoot, recursive: true);
        }
    }
}

[TestFixture]
public class UpdateDatabaseCommandTests
{
    [Test]
    public void ExecuteInternal_WhenConnectionUnreachable_ReturnsFailureCode()
    {
        var scriptRoot = Path.Combine(Path.GetTempPath(), "crap-update-exec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(scriptRoot, "Update"));
        try
        {
            var cmd = new UpdateDatabaseCommand();
            var method = typeof(UpdateDatabaseCommand).GetMethod(
                "ExecuteInternal",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method.ShouldNotBeNull();

            var options = new DatabaseOptions
            {
                ScriptDir = scriptRoot,
                DatabaseName = "CrapGateUpdate",
                SuppressUpgradeConsoleOutput = true
            };
            var connectionString =
                "Server=127.0.0.1,1;Database=ChurchBulletinCrapGate;User ID=sa;Password=invalid;" +
                "TrustServerCertificate=true;Encrypt=false;Connect Timeout=1;";

            var result = (int)method.Invoke(
                cmd,
                [null!, options, connectionString, CancellationToken.None])!;

            result.ShouldBe(-1);
        }
        finally
        {
            Directory.Delete(scriptRoot, recursive: true);
        }
    }
}
