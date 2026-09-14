using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Client;

/// <summary>
/// Adapter that exposes <see cref="IWebAssemblyHostEnvironment"/> as <see cref="IHostEnvironment"/>
/// so that shared UI components can inject <see cref="IHostEnvironment"/> on both the server and
/// the Blazor WebAssembly client without conditional compilation.
/// </summary>
internal sealed class WasmHostEnvironment(IWebAssemblyHostEnvironment inner) : IHostEnvironment
{
    public string EnvironmentName
    {
        get => inner.Environment;
        set => throw new NotSupportedException();
    }

    public string ApplicationName { get; set; } = string.Empty;
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
