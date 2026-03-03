using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.Messages;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UnitTests;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

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
                s.AddTransient<IToolProvider, InProcessToolProvider>();
            })
            .UseNServiceBus(context =>
            {
                var endpointConfiguration = new EndpointConfiguration("IntegrationTests");
                endpointConfiguration.UseSerialization<SystemJsonSerializer>();
                endpointConfiguration.EnableInstallers();

                var connectionString = context.Configuration.GetConnectionString("SqlConnectionString") ?? "";
                if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    var learningTransport = endpointConfiguration.UseTransport<LearningTransport>();
                    learningTransport.Routing()
                        .RouteToEndpoint(typeof(TracerBulletCommand), "WorkOrderProcessing");
                }
                else
                {
                    var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
                    transport.ConnectionString(connectionString);
                    transport.DefaultSchema("nServiceBus");
                    transport.Transactions(TransportTransactionMode.TransactionScope);
                    transport.Routing()
                        .RouteToEndpoint(typeof(TracerBulletCommand), "WorkOrderProcessing");
                }

                var conventions = new MessagingConventions();
                endpointConfiguration.Conventions().Add(conventions);

                return endpointConfiguration;
            })
            .Build();

        host.Start();
        _host = host;
    }

    private class StubTimeProvider(DateTimeOffset testTime) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return testTime;
        }
    }

    /// <summary>
    /// Provides tools directly via AIFunctionFactory for integration tests
    /// that don't have a running MCP server.
    /// </summary>
    private class InProcessToolProvider(IServiceProvider serviceProvider) : IToolProvider
    {
        private IBus CreateScopedBus()
        {
            var scope = serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IBus>();
        }

        private IWorkOrderNumberGenerator CreateScopedNumberGenerator()
        {
            var scope = serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IWorkOrderNumberGenerator>();
        }

        public Task<IList<AITool>> GetToolsAsync()
        {
            IList<AITool> tools =
            [
                AIFunctionFactory.Create(
                    ([System.ComponentModel.Description("Optional status filter")] string? status = null)
                        => WorkOrderTools.ListWorkOrders(CreateScopedBus(), status),
                    "ListWorkOrders",
                    "Lists all work orders, optionally filtered by status."),
                AIFunctionFactory.Create(
                    ([System.ComponentModel.Description("The work order number")] string workOrderNumber)
                        => WorkOrderTools.GetWorkOrder(CreateScopedBus(), workOrderNumber),
                    "GetWorkOrder",
                    "Retrieves a single work order by its number."),
                AIFunctionFactory.Create(
                    ([System.ComponentModel.Description("Title")] string title,
                     [System.ComponentModel.Description("Description")] string description,
                     [System.ComponentModel.Description("Creator username")] string creatorUsername,
                     [System.ComponentModel.Description("Optional room number")] string? roomNumber = null)
                        => WorkOrderTools.CreateWorkOrder(CreateScopedBus(), CreateScopedNumberGenerator(), title, description, creatorUsername, roomNumber),
                    "CreateWorkOrder",
                    "Creates a new draft work order."),
                AIFunctionFactory.Create(
                    ([System.ComponentModel.Description("Work order number")] string workOrderNumber,
                     [System.ComponentModel.Description("Command name")] string commandName ,
                     [System.ComponentModel.Description("Executing username")] string executingUsername,
                     [System.ComponentModel.Description("Assignee username")] string? assigneeUsername = null)
                        => WorkOrderTools.ExecuteWorkOrderCommand(CreateScopedBus(), workOrderNumber, commandName, executingUsername, assigneeUsername),
                    "ExecuteWorkOrderCommand",
                    "Executes a state command on a work order."),
                AIFunctionFactory.Create(
                    () => EmployeeTools.ListEmployees(CreateScopedBus()),
                    "ListEmployees",
                    "Lists all employees."),
                AIFunctionFactory.Create(
                    ([System.ComponentModel.Description("Username")] string username)
                        => EmployeeTools.GetEmployee(CreateScopedBus(), username),
                    "GetEmployee",
                    "Retrieves a single employee by username."),
            ];
            return Task.FromResult(tools);
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