using System.Reflection;
using NServiceBus;
using NServiceBus.Extensibility;

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

        var name = targetMethod.Name;
        if ((name is "SendLocal" or "Send") && args is { Length: > 0 } && args[0] is { } sent)
        {
            _sentLocal.Add(sent);
            return CompletedFor(targetMethod.ReturnType);
        }

        if (name == "Reply" && args is { Length: > 0 } && args[0] is { } replied)
        {
            _replied.Add(replied);
            return CompletedFor(targetMethod.ReturnType);
        }

        if (name == "Publish" && args is { Length: > 0 } && args[0] is { } published)
        {
            _published.Add(published);
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
            return new ContextBag();
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
