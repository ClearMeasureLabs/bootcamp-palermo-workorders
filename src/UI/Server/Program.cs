var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Host.UseLamar(registry => { registry.IncludeRegistry<UiServiceRegistry>(); });
builder.Services.AddSingleton(TimeProvider.System);

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