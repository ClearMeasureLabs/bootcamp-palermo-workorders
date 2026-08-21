using System.Reflection;
using NServiceBus;

namespace ClearMeasure.Bootcamp.UnitTests.Worker;

/// <summary>
/// Lightweight <see cref="IMessageHandlerContext"/> double that records SendLocal/Reply/Publish
/// without taking a dependency on NServiceBus.Testing.
/// </summary>
public class StubMessageHandlerContext : DispatchProxy
{
    private readonly List<object> _sentLocal = [];
    private readonly List<object> _replied = [];
    private readonly List<object> _published = [];

    public IReadOnlyList<object> SentLocalMessages => _sentLocal;
    public IReadOnlyList<object> RepliedMessages => _replied;
    public IReadOnlyList<object> PublishedMessages => _published;

    public IMessageHandlerContext Context { get; private set; } = null!;

    public static StubMessageHandlerContext Create()
    {
        var context = Create<IMessageHandlerContext, StubMessageHandlerContext>();
        // DispatchProxy.Create returns TInterface backed by TProxy.
        // ReSharper disable once SuspiciousTypeConversion.Global
        var stub = (StubMessageHandlerContext)context;
        stub.Context = context;
        return stub;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

        var name = targetMethod.Name;
        if ((name is "SendLocal" or "Send") && args is { Length: > 0 } && args[0] is not null)
        {
            _sentLocal.Add(args[0]!);
            return CompletedFor(targetMethod.ReturnType);
        }

        if (name == "Reply" && args is { Length: > 0 } && args[0] is not null)
        {
            _replied.Add(args[0]!);
            return CompletedFor(targetMethod.ReturnType);
        }

        if (name == "Publish" && args is { Length: > 0 } && args[0] is not null)
        {
            _published.Add(args[0]!);
            return CompletedFor(targetMethod.ReturnType);
        }

        if (targetMethod.ReturnType == typeof(CancellationToken))
        {
            return CancellationToken.None;
        }

        if (name is "get_MessageId" or "get_ReplyToAddress")
        {
            return string.Empty;
        }

        if (name == "get_MessageHeaders")
        {
            return new Dictionary<string, string>();
        }

        if (name == "get_Extensions")
        {
            return Activator.CreateInstance(
                Type.GetType("NServiceBus.Extensibility.ContextBag, NServiceBus")
                ?? throw new InvalidOperationException("NServiceBus ContextBag type not found."));
        }

        return CompletedFor(targetMethod.ReturnType);
    }

    private static object? CompletedFor(Type returnType)
    {
        if (returnType == typeof(void) || returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType);
            return fromResult.Invoke(null, [GetDefault(resultType)]);
        }

        return GetDefault(returnType);
    }

    private static object? GetDefault(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}
