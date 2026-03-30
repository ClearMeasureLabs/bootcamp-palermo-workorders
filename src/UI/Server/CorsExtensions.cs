using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Registers a named CORS policy from <see cref="ServerCorsOptions"/>.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Adds CORS services and a policy named <see cref="ServerCorsOptions.PolicyName"/> when origins are configured.
    /// </summary>
    public static IServiceCollection AddServerCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServerCorsOptions>(configuration.GetSection(ServerCorsOptions.SectionName));

        var snapshot = configuration.GetSection(ServerCorsOptions.SectionName).Get<ServerCorsOptions>() ?? new ServerCorsOptions();
        if (!snapshot.Enabled || snapshot.AllowedOrigins.Length == 0)
        {
            return services;
        }

        services.AddCors(frameworkOptions => ConfigurePolicy(frameworkOptions, snapshot.AllowedOrigins));
        return services;
    }

    private static void ConfigurePolicy(CorsOptions options, string[] allowedOrigins)
    {
        options.AddPolicy(
            ServerCorsOptions.PolicyName,
            builder => builder.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
    }

    /// <summary>
    /// Returns whether the app should use CORS middleware and controller metadata for the current configuration.
    /// </summary>
    public static bool IsServerCorsActive(this IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<ServerCorsOptions>>().Value;
        return options.Enabled && options.AllowedOrigins.Length > 0;
    }
}
