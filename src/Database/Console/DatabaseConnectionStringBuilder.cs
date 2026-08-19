using Microsoft.Data.SqlClient;

namespace ClearMeasure.Bootcamp.Database.Console;

/// <summary>
/// Builds SQL Server connection strings for DbUp database commands.
/// </summary>
public static class DatabaseConnectionStringBuilder
{
    private static readonly string[] LocalServerIndicators =
    [
        "localhost",
        "127.0.0.1",
        "localdb",
        "(localdb)"
    ];

    /// <summary>
    /// Builds a connection string from database command options.
    /// </summary>
    public static string Build(DatabaseOptions options)
    {
        var serverName = (options.DatabaseServer ?? string.Empty).Trim();
        var isLocalServer = IsLocalServer(serverName);
        var isLocalDb = IsLocalDb(serverName);
        var dataSource = FormatDataSource(serverName, isLocalDb);

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = options.DatabaseName,
            ConnectTimeout = 60
        };

        ApplySecuritySettings(builder, isLocalServer);
        ApplyAuthentication(builder, options);

        return builder.ToString();
    }

    public static bool IsLocalServer(string serverName)
    {
        return LocalServerIndicators.Any(indicator =>
            serverName.Contains(indicator, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsLocalDb(string serverName)
    {
        return serverName.Contains("localdb", StringComparison.OrdinalIgnoreCase)
               || serverName.Contains("(localdb)", StringComparison.OrdinalIgnoreCase);
    }

    public static string FormatDataSource(string dataSource, bool isLocalDb)
    {
        if (isLocalDb || HasPortOrInstance(dataSource))
        {
            return dataSource;
        }

        return $"{dataSource},1433";
    }

    internal static bool HasPortOrInstance(string dataSource)
    {
        return dataSource.Contains(',')
               || dataSource.Contains(':')
               || dataSource.Contains('\\');
    }

    internal static void ApplySecuritySettings(SqlConnectionStringBuilder builder, bool isLocalServer)
    {
        if (isLocalServer)
        {
            builder.Encrypt = SqlConnectionEncryptOption.Optional;
            builder.TrustServerCertificate = true;
            return;
        }

        builder.Encrypt = SqlConnectionEncryptOption.Mandatory;
        builder.TrustServerCertificate = false;
    }

    internal static void ApplyAuthentication(SqlConnectionStringBuilder builder, DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DatabaseUser))
        {
            builder.IntegratedSecurity = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(options.DatabasePassword))
        {
            throw new ArgumentException(
                "DatabasePassword is required when DatabaseUser is provided",
                nameof(DatabaseOptions.DatabasePassword));
        }

        builder.IntegratedSecurity = false;
        builder.UserID = options.DatabaseUser;
        builder.Password = options.DatabasePassword;
    }
}
