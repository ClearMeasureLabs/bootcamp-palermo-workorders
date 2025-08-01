using System.Net.Http.Json;
using BlazorApplicationInsights;
using ClearMeasure.Bootcamp.UI.Client;
using Lamar;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ClearMeasure.Bootcamp.Core;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
builder.Services.AddScoped(sp => http);
ConfigurationModel? ConfigurationModel = new ConfigurationModel() { AppInsightsConnectionString = "" };//await http.GetFromJsonAsync<ConfigurationModel>("Configuration");}

if (ConfigurationModel != null)
    builder.Services.AddBlazorApplicationInsights(x =>
    {
        x.ConnectionString = ConfigurationModel.AppInsightsConnectionString;
    });

// Add authentication services
builder.Services.AddAuthorizationCore();
builder.ConfigureContainer<ServiceRegistry>(
    new LamarServiceProviderFactory(), registry =>
        registry.IncludeRegistry<UIClientServiceRegistry>());


var url = builder.Configuration.GetValue<string>("RemoteBusUrl") ??
          throw new InvalidOperationException("Must have config value 'RemoteBusUrl'");
builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(url) });
var app = builder.Build();
await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();
await app.RunAsync();