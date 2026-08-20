using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ProblemDetailsExceptionHandlerTests
{
    [Test]
    public async Task HandleAsync_ShouldIncludeDetail_WhenDevelopmentAndExceptionPresent()
    {
        var written = new List<ProblemDetailsContext>();
        var context = CreateHttpContext(written, new InvalidOperationException("boom"));

        await ProblemDetailsExceptionHandler.HandleAsync(context, new StubHostEnvironment(Environments.Development));

        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        written.Count.ShouldBe(1);
        written[0].ProblemDetails.Detail.ShouldNotBeNullOrEmpty();
        written[0].ProblemDetails.Detail!.ShouldContain("boom");
    }

    [Test]
    public async Task HandleAsync_ShouldOmitDetail_WhenNotDevelopment()
    {
        var written = new List<ProblemDetailsContext>();
        var context = CreateHttpContext(written, new InvalidOperationException("secret"));

        await ProblemDetailsExceptionHandler.HandleAsync(context, new StubHostEnvironment(Environments.Production));

        written.Count.ShouldBe(1);
        written[0].ProblemDetails.Detail.ShouldBeNull();
        written[0].ProblemDetails.Status.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Test]
    public async Task HandleAsync_ShouldOmitDetail_WhenExceptionMissing()
    {
        var written = new List<ProblemDetailsContext>();
        var context = CreateHttpContext(written, exception: null);

        await ProblemDetailsExceptionHandler.HandleAsync(context, new StubHostEnvironment(Environments.Development));

        written.Count.ShouldBe(1);
        written[0].ProblemDetails.Detail.ShouldBeNull();
    }

    private static DefaultHttpContext CreateHttpContext(List<ProblemDetailsContext> written, Exception? exception)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IProblemDetailsService>(new StubProblemDetailsService(written));
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };
        if (exception != null)
        {
            context.Features.Set<IExceptionHandlerPathFeature>(new StubExceptionFeature(exception));
        }

        return context;
    }

    private sealed class StubProblemDetailsService(List<ProblemDetailsContext> written) : IProblemDetailsService
    {
        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            written.Add(context);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StubExceptionFeature(Exception error) : IExceptionHandlerPathFeature
    {
        public Exception Error => error;
        public string Path => "/api/test";
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "UnitTests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
