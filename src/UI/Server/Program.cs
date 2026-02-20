using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Host.UseLamar(registry => { registry.IncludeRegistry<UiServiceRegistry>(); });
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IDistributedBus, DistributedBus>();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add NServiceBus endpoint
var endpointConfiguration = new NServiceBus.EndpointConfiguration("UI.Server");
endpointConfiguration.UseSerialization<SystemJsonSerializer>();
endpointConfiguration.EnableInstallers();
endpointConfiguration.EnableOpenTelemetry();

// transport
var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
transport.ConnectionString(builder.Configuration.GetConnectionString("SqlConnectionString"));
transport.DefaultSchema("nServiceBus");
transport.Transactions(TransportTransactionMode.TransactionScope);

// message conventions
var conventions = new MessagingConventions();
endpointConfiguration.Conventions().Add(conventions);

builder.Host.UseNServiceBus(_ => endpointConfiguration);

// Build application
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapHealthChecks("_healthcheck");

await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();

app.Run();