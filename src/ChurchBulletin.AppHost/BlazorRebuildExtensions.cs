using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace ClearMeasure.Bootcamp.ChurchBulletin.AppHost;

/// <summary>
/// Workaround for <see href="https://github.com/microsoft/aspire/issues/20709"/>: on the .NET 11 SDK,
/// Aspire's managed project build coordinator appends <c>-mt</c> (multithreaded MSBuild), which the
/// WebAssembly SDK's <c>ReadWasmProps</c> custom task factory cannot execute out of process. This
/// strips <c>-mt</c> from the coordinated build so Blazor WebAssembly clients build and run again.
/// Remove once Aspire stops injecting <c>-mt</c> (or the WebAssembly SDK supports out-of-process execution).
/// </summary>
public static class BlazorRebuildExtensions
{
    /// <summary>
    /// Removes Aspire's multithreaded MSBuild switch from the local build required by this project.
    /// </summary>
    /// <param name="project">The dotnet project builder whose build depends on a Blazor WebAssembly client.</param>
    /// <returns>The unchanged project builder.</returns>
    public static IResourceBuilder<T> WithSingleProcessRebuild<T>(this IResourceBuilder<T> project)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(project);

        var builder = project.ApplicationBuilder;
        if (builder.ExecutionContext.IsPublishMode)
        {
            return project;
        }

        // Subscribe after Aspire's own eventing subscribers so the argument filter runs after its -mt injection.
        builder.Eventing.Subscribe<BeforeStartEvent>((_, _) =>
        {
            builder.Eventing.Subscribe<BeforeResourceStartedEvent>((@event, _) =>
            {
                if (@event.Resource is ExecutableResource { Command: "dotnet" } executable
                    && (executable is IResourceWithParent parent
                        && ReferenceEquals(parent.Parent, project.Resource)
                        || project.Resource.Annotations.OfType<WaitAnnotation>().Any(wait =>
                            wait.WaitType == WaitType.WaitForCompletion
                            && ReferenceEquals(wait.Resource, executable))))
                {
                    lock (executable.Annotations)
                    {
                        if (!executable.Annotations.OfType<SingleProcessBuildArgsAnnotation>().Any())
                        {
                            executable.Annotations.Add(new SingleProcessBuildArgsAnnotation());
                        }
                    }
                }

                return Task.CompletedTask;
            });

            return Task.CompletedTask;
        });

        return project;
    }

    private sealed class SingleProcessBuildArgsAnnotation : CommandLineArgsCallbackAnnotation
    {
        public SingleProcessBuildArgsAnnotation()
            : base(context =>
            {
                if (context.Args.Count == 0 || context.Args[0] is not "build")
                {
                    return Task.CompletedTask;
                }

                for (var index = context.Args.Count - 1; index >= 0; index--)
                {
                    if (context.Args[index] is "-mt")
                    {
                        context.Args.RemoveAt(index);
                    }
                }

                return Task.CompletedTask;
            })
        {
        }
    }
}
