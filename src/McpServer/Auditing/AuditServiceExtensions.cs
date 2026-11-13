namespace ClearMeasure.Bootcamp.McpServer.Auditing;

/// <summary>Registers retention consistently for both HTTP and standalone MCP hosts.</summary>
public static class AuditServiceExtensions
{
    /// <summary>Adds validated retention settings and its cancellation-aware worker.</summary>
    public static IServiceCollection AddAuditRetention(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuditRetentionOptions>()
            .Bind(configuration.GetSection("AuditLog"))
            .Validate(options => options.RetentionDays is > 0 and <= 36500,
                "AuditLog:RetentionDays must be between 1 and 36500.")
            .ValidateOnStart();
        services.AddHttpContextAccessor();
        services.AddHostedService<AuditRetentionService>();
        return services;
    }
}
