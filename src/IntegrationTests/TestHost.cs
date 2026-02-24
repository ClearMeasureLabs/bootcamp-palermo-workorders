using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UnitTests;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.IntegrationTests;

public static class TestHost
{
    public static DateTimeOffset TestTime { get; set; } = new(2000, 1, 1, 1, 1, 1, TimeSpan.Zero);
    private static bool _dependenciesRegistered;
    private static readonly object Lock = new();
    private static IHost? _host;

    public static IHost Instance
    {
        get
        {
            EnsureDependenciesRegistered();
            return _host!;
        }
    }

    public static T GetRequiredService<T>(bool newScope = true) where T : notnull
    {
        EnsureDependenciesRegistered();
        if (newScope)
        {
            var serviceScope = Instance.Services.CreateScope();
            var provider = serviceScope.ServiceProvider;
            return provider.GetRequiredService<T>();
        }

        return Instance.Services.GetRequiredService<T>();
    }

    private static void Initialize()
    {
        var host = Host.CreateDefaultBuilder()
            .UseEnvironment("Development")
            .UseLamar(registry => { registry.IncludeRegistry<UiServiceRegistry>(); })
            .ConfigureLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .ConfigureAppConfiguration((context, config) =>
            {
                var env = context.HostingEnvironment;

                config
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                    .AddJsonFile("appsettings.acceptancetests.json", true, true)
                    .AddJsonFile("appsettings.test.json", false, true)
                    .AddUserSecrets<TestDatabaseConfiguration>(optional: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices(s =>
            {
                s.AddTransient<IDatabaseConfiguration, TestDatabaseConfiguration>();
                var stubTimeProvider = new StubTimeProvider(TestTime);
                s.AddSingleton<TimeProvider>(stubTimeProvider);
                s.AddScoped<IDistributedBus, DistributedBus>();
            })
            .UseNServiceBus(context =>
            {
                var endpointConfiguration = new EndpointConfiguration("IntegrationTests");
                endpointConfiguration.UseSerialization<SystemJsonSerializer>();
                endpointConfiguration.EnableInstallers();
                endpointConfiguration.SendOnly();

                var connectionString = context.Configuration.GetConnectionString("SqlConnectionString") ?? "";
                if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    endpointConfiguration.UseTransport<LearningTransport>();
                }
                else
                {
                    var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
                    transport.ConnectionString(connectionString);
                    transport.DefaultSchema("nServiceBus");
                    transport.Transactions(TransportTransactionMode.TransactionScope);
                }

                var conventions = new MessagingConventions();
                endpointConfiguration.Conventions().Add(conventions);

                return endpointConfiguration;
            })
            .Build();


        _host = host;
    }

    private class StubTimeProvider(DateTimeOffset testTime) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return testTime;
        }
    }

    private static void EnsureDependenciesRegistered()
    {
        if (!_dependenciesRegistered)
        {
            lock (Lock)
            {
                if (!_dependenciesRegistered)
                {
                    Initialize();
                    _dependenciesRegistered = true;
                }
            }
        }
    }

    public static DataContext NewDbContext()
    {
        return GetRequiredService<DataContext>();
    }

    public static TK Faker<TK>()
    {
        return ObjectMother.Faker<TK>();
    }
}