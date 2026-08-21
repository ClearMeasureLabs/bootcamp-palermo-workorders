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
    private readonly Dictionary<string, Func<object?[]?, Type, object?>> _handlers;

    public IReadOnlyList<object> SentLocalMessages => _sentLocal;
    public IReadOnlyList<object> RepliedMessages => _replied;
    public IReadOnlyList<object> PublishedMessages => _published;

    public IMessageHandlerContext Context { get; private set; } = null!;

    public StubMessageHandlerContext()
    {
        _handlers = new Dictionary<string, Func<object?[]?, Type, object?>>(StringComparer.Ordinal)
        {
            ["SendLocal"] = RecordSentLocal,
            ["Send"] = RecordSentLocal,
            ["Reply"] = RecordReply,
            ["Publish"] = RecordPublish,
            ["get_MessageId"] = static (_, _) => string.Empty,
            ["get_ReplyToAddress"] = static (_, _) => string.Empty,
            ["get_MessageHeaders"] = static (_, _) => new Dictionary<string, string>(),
            ["get_Extensions"] = static (_, _) => CreateContextBag()
        };
    }

    public static StubMessageHandlerContext Create()
    {
        // Box the DispatchProxy result so recovering TProxy uses a normal type test,
        // not a dual-inheritance cast (SuspiciousTypeConversion) or a cast Qodana
        // treats as redundant when applied directly to the interface-typed local.
        object proxy = Create<IMessageHandlerContext, StubMessageHandlerContext>();
        var stub = proxy as StubMessageHandlerContext
            ?? throw new InvalidOperationException("DispatchProxy did not create StubMessageHandlerContext.");
        stub.Context = (IMessageHandlerContext)proxy;
        return stub;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

        if (targetMethod.ReturnType == typeof(CancellationToken))
        {
            return CancellationToken.None;
        }

        if (_handlers.TryGetValue(targetMethod.Name, out var handler))
        {
            return handler(args, targetMethod.ReturnType);
        }

        return CompletedFor(targetMethod.ReturnType);
    }

    private object? RecordSentLocal(object?[]? args, Type returnType) =>
        RecordMessage(args, _sentLocal, returnType);

    private object? RecordReply(object?[]? args, Type returnType) =>
        RecordMessage(args, _replied, returnType);

    private object? RecordPublish(object?[]? args, Type returnType) =>
        RecordMessage(args, _published, returnType);

    private static object? RecordMessage(object?[]? args, List<object> sink, Type returnType)
    {
        if (args is { Length: > 0 } && args[0] is { } message)
        {
            sink.Add(message);
        }

        return CompletedFor(returnType);
    }

    private static object CreateContextBag()
    {
        var type = Type.GetType("NServiceBus.Extensibility.ContextBag, NServiceBus.Core")
            ?? Type.GetType("NServiceBus.Extensibility.ContextBag, NServiceBus")
            ?? throw new InvalidOperationException("NServiceBus ContextBag type not found.");
        return System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
    }
    private static object? CompletedFor(Type returnType)
    {
        if (returnType == typeof(void) || returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return CreateCompletedGenericTask(returnType);
        }

        return GetDefault(returnType);
    }

    private static object? CreateCompletedGenericTask(Type returnType)
    {
        var resultType = returnType.GetGenericArguments()[0];
        var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType);
        return fromResult.Invoke(null, [GetDefault(resultType)]);
    }

    private static object? GetDefault(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}
