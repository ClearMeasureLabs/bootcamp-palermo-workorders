using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class CanConnectToLlmServerHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenNotConfigured_ReturnsHealthyWithInfo()
    {
        var healthCheck = CreateHealthCheck(new StubChatClientFactory(
            new ChatClientAvailabilityResult(false, "missing configuration")));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("missing");
        result.Description.ShouldContain("not enabled in this environment");
    }

    [Test]
    public async Task CheckHealthAsync_WhenChatSucceeds_ReturnsHealthy()
    {
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]);
        var healthCheck = CreateHealthCheck(new StubChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            response));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenChatEmpty_ReturnsDegraded()
    {
        var healthCheck = CreateHealthCheck(new StubChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            new ChatResponse([])));

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Test]
    public async Task CheckHealthAsync_WhenChatThrows_ReturnsUnhealthy()
    {
        var healthCheck = CreateHealthCheck(new ThrowingChatClientFactory());

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("Chat client connection failed");
    }

    [Test]
    public async Task CheckHealthAsync_WhenCacheHit_DoesNotCallProbeAgain()
    {
        var factory = new CountingChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]));
        var cache = new LlmHealthCheckCacheStub();
        var healthCheck = CreateHealthCheck(factory, cache, window: TimeSpan.FromSeconds(30));

        await healthCheck.CheckHealthAsync(CreateContext(healthCheck));
        await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        factory.GetChatClientCallCount.ShouldBe(1);
    }

    [Test]
    public async Task CheckHealthAsync_WhenCacheExpired_CallsProbeAgain()
    {
        var factory = new CountingChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]));
        var now = DateTimeOffset.UtcNow;
        var fakeClock = new FakeClock(now);
        var realCache = new LlmHealthCheckCache(fakeClock.Now);
        var healthCheck = CreateHealthCheck(factory, realCache, window: TimeSpan.FromSeconds(30));

        await healthCheck.CheckHealthAsync(CreateContext(healthCheck)); // populates cache
        fakeClock.Advance(TimeSpan.FromSeconds(60));                    // expire the cache
        await healthCheck.CheckHealthAsync(CreateContext(healthCheck)); // should re-probe

        factory.GetChatClientCallCount.ShouldBe(2);
    }

    [Test]
    public async Task CheckHealthAsync_WhenProbeFails_ResultIsNotCached()
    {
        var factory = new CountingThrowingChatClientFactory();
        var cache = new LlmHealthCheckCacheStub();
        var healthCheck = CreateHealthCheck(factory, cache, window: TimeSpan.FromSeconds(30));

        await healthCheck.CheckHealthAsync(CreateContext(healthCheck));
        await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        factory.GetChatClientCallCount.ShouldBe(2);
    }

    [Test]
    public async Task CheckHealthAsync_WhenNotConfigured_BypassesCache()
    {
        var factory = new StubChatClientFactory(new ChatClientAvailabilityResult(false, "not configured"));
        var cache = new LlmHealthCheckCacheStub();
        var healthCheck = CreateHealthCheck(factory, cache, window: TimeSpan.FromSeconds(30));

        await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        cache.SetCallCount.ShouldBe(0);
    }

    [Test]
    public async Task CheckHealthAsync_OnCacheHit_AnnotatesResultWithCachedTrueAndAgeSeconds()
    {
        var factory = new CountingChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]));
        var cache = new LlmHealthCheckCacheStub();
        var healthCheck = CreateHealthCheck(factory, cache, window: TimeSpan.FromSeconds(30));

        await healthCheck.CheckHealthAsync(CreateContext(healthCheck)); // miss — populates stub
        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck)); // hit

        result.Data["cached"].ShouldBe(true);
        ((double)result.Data["cache_age_seconds"]).ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task CheckHealthAsync_OnFreshProbe_AnnotatesResultWithCachedFalseAndZeroAge()
    {
        var factory = new CountingChatClientFactory(
            new ChatClientAvailabilityResult(true, "configured"),
            new ChatResponse([new ChatMessage(ChatRole.Assistant, "OK")]));
        var healthCheck = CreateHealthCheck(factory);

        var result = await healthCheck.CheckHealthAsync(CreateContext(healthCheck));

        result.Data["cached"].ShouldBe(false);
        result.Data["cache_age_seconds"].ShouldBe((double)0);
    }

    private static CanConnectToLlmServerHealthCheck CreateHealthCheck(
        ChatClientFactory factory,
        ILlmHealthCheckCache? cache = null,
        TimeSpan? window = null)
    {
        var opts = Options.Create(new LlmHealthCheckOptions
        {
            CacheDuration = window ?? TimeSpan.FromMinutes(5)
        });
        return new CanConnectToLlmServerHealthCheck(
            factory,
            cache ?? new LlmHealthCheckCache(),
            opts,
            NullLogger<CanConnectToLlmServerHealthCheck>.Instance);
    }

    private static HealthCheckContext CreateContext(CanConnectToLlmServerHealthCheck healthCheck) =>
        new()
        {
            Registration = new HealthCheckRegistration("LlmGateway", healthCheck, null, null)
        };

    private sealed class StubChatClientFactory(
        ChatClientAvailabilityResult availability,
        ChatResponse? response = null) : ChatClientFactory(null!)
    {
        public override Task<ChatClientAvailabilityResult> IsChatClientAvailable() =>
            Task.FromResult(availability);

        public override Task<IChatClient> GetChatClient() =>
            Task.FromResult<IChatClient>(new StubHealthChatClient(response ?? new ChatResponse([])));
    }

    private sealed class CountingChatClientFactory(
        ChatClientAvailabilityResult availability,
        ChatResponse response) : ChatClientFactory(null!)
    {
        public int GetChatClientCallCount;

        public override Task<ChatClientAvailabilityResult> IsChatClientAvailable() =>
            Task.FromResult(availability);

        public override Task<IChatClient> GetChatClient()
        {
            GetChatClientCallCount++;
            return Task.FromResult<IChatClient>(new StubHealthChatClient(response));
        }
    }

    private sealed class CountingThrowingChatClientFactory() : ChatClientFactory(null!)
    {
        public int GetChatClientCallCount;

        public override Task<ChatClientAvailabilityResult> IsChatClientAvailable() =>
            Task.FromResult(new ChatClientAvailabilityResult(true, "configured"));

        public override Task<IChatClient> GetChatClient()
        {
            GetChatClientCallCount++;
            throw new InvalidOperationException("probe-failed");
        }
    }

    private sealed class ThrowingChatClientFactory() : ChatClientFactory(null!)
    {
        public override Task<ChatClientAvailabilityResult> IsChatClientAvailable() =>
            Task.FromResult(new ChatClientAvailabilityResult(true, "configured"));

        public override Task<IChatClient> GetChatClient() =>
            throw new InvalidOperationException("probe-failed");
    }

    private sealed class StubHealthChatClient(ChatResponse response) : IChatClient
    {
        public void Dispose()
        {
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(response);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }

    /// <summary>
    /// A stub cache that always reports a miss on first call, then stores the result and serves it on subsequent calls.
    /// Used so that unit tests can verify caching behavior without relying on real time.
    /// </summary>
    private sealed class LlmHealthCheckCacheStub : ILlmHealthCheckCache
    {
        private HealthCheckResult? _stored;
        public int SetCallCount;

        public bool TryGet(TimeSpan window, out HealthCheckResult cached, out double ageSeconds)
        {
            if (_stored is null)
            {
                cached = default;
                ageSeconds = 0;
                return false;
            }

            cached = _stored.Value;
            ageSeconds = 1;
            return true;
        }

        public void Set(HealthCheckResult result, DateTimeOffset now)
        {
            SetCallCount++;
            _stored = result;
        }
    }

    private sealed class FakeClock(DateTimeOffset initial)
    {
        private DateTimeOffset _current = initial;

        public DateTimeOffset Now() => _current;

        public void Advance(TimeSpan delta) => _current += delta;
    }
}
