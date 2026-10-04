using ClearMeasure.Bootcamp.UI.Client;
using Lamar;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Toolbelt.Blazor.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<IHostEnvironment>(new WasmHostEnvironment(builder.HostEnvironment));

// Add authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddSpeechSynthesis();
builder.Services.AddSpeechRecognition();
builder.ConfigureContainer<ServiceRegistry>(
    new LamarServiceProviderFactory(), registry =>
        registry.IncludeRegistry<UIClientServiceRegistry>());


var app = builder.Build();
await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();
await app.RunAsync();