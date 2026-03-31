using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Registers ASP.NET Core request timeout policies for API routes matched by <see cref="ApiRateLimitingExtensions.ShouldApplyToPath"/>.
/// </summary>
public static class ApiRequestTimeoutsExtensions
{
    /// <summary>Named policy applied to MVC API controllers when timeouts are enabled.</summary>
    public const string ApiControllersPolicyName = "ApiControllers";

    /// <summary>
    /// Adds request timeout services and an API policy when <see cref="ApiRequestTimeoutOptions"/> is enabled with a positive timeout.
    /// </summary>
    public static IServiceCollection AddApiRequestTimeouts(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiRequestTimeoutOptions>(configuration.GetSection(ApiRequestTimeoutOptions.SectionName));
        services.AddRequestTimeouts();
        services.AddSingleton<IConfigureOptions<RequestTimeoutOptions>, ConfigureApiRequestTimeoutPolicyOptions>();
        return services;
    }

    private sealed class ConfigureApiRequestTimeoutPolicyOptions : IConfigureOptions<RequestTimeoutOptions>
    {
        private readonly IOptions<ApiRequestTimeoutOptions> _apiOptions;

        public ConfigureApiRequestTimeoutPolicyOptions(IOptions<ApiRequestTimeoutOptions> apiOptions)
        {
            _apiOptions = apiOptions;
        }

        public void Configure(RequestTimeoutOptions options)
        {
            var api = _apiOptions.Value;
            if (!api.Enabled || api.TimeoutSeconds <= 0)
            {
                return;
            }

            var timeout = TimeSpan.FromSeconds(api.TimeoutSeconds);
            options.AddPolicy(
                ApiControllersPolicyName,
                new RequestTimeoutPolicy
                {
                    Timeout = timeout,
                    TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
                });
        }
    }
}
