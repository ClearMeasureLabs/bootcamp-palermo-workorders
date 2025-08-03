using BlazorApplicationInsights;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Client;
using Lamar;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
builder.Services.AddScoped(sp => http);
var configurationModel = new ConfigurationModel
    { AppInsightsConnectionString = "" }; //await http.GetFromJsonAsync<ConfigurationModel>("Configuration");}

builder.Services.AddBlazorApplicationInsights(x =>
{
    x.ConnectionString = configurationModel.AppInsightsConnectionString;
});

// Add authentication services
builder.Services.AddAuthorizationCore();
builder.ConfigureContainer<ServiceRegistry>(
    new LamarServiceProviderFactory(), registry =>
        registry.IncludeRegistry<UIClientServiceRegistry>());


var url = builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(url) });
var app = builder.Build();
await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();
await app.RunAsync();