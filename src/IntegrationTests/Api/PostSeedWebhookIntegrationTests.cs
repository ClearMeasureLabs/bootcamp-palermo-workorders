using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class PostSeedWebhookIntegrationTests
{
    private const string MainSharedDbName = "post-seed-webhook-main";

    private SqliteConnection? _sharedMemoryHold;
    private PostSeedWebhookWebApplicationFactory? _factory;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var cs = $"Data Source={MainSharedDbName};Mode=Memory;Cache=Shared";
        _sharedMemoryHold = new SqliteConnection(cs);
        _sharedMemoryHold.Open();
        _factory = new PostSeedWebhookWebApplicationFactory(MainSharedDbName);
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
        _sharedMemoryHold?.Dispose();
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task Should_Return200_When_PostSeedWebhook_UnversionedAndVersioned()
    {
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            db.Database.EnsureCreated();
            db.Add(new Employee("seed-user", "Seed", "User", "seed@t.test"));
            db.SaveChanges();
        }

        var payload = new PostSeedWebhookRequest("post-seed", Guid.NewGuid().ToString(), null);

        var unversioned = await _client!.PostAsJsonAsync("/api/post-seed-webhook", payload);
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body1 = await unversioned.Content.ReadFromJsonAsync<PostSeedWebhookResponse>(JsonOptions);
        body1.ShouldNotBeNull();
        body1!.Received.ShouldBeTrue();
        body1.SeedDataDetected.ShouldBeTrue();

        var versioned = await _client.PostAsJsonAsync("/api/v1.0/post-seed-webhook", payload);
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body2 = await versioned.Content.ReadFromJsonAsync<PostSeedWebhookResponse>(JsonOptions);
        body2.ShouldNotBeNull();
        body2!.Received.ShouldBeTrue();
        body2.SeedDataDetected.ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithSeedDataDetectedFalse_When_PostSeedWebhook_And_NoEmployees()
    {
        const string dbName = "post-seed-webhook-empty";
        var cs = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        await using var hold = new SqliteConnection(cs);
        await hold.OpenAsync();
        await using var isolatedFactory = new PostSeedWebhookWebApplicationFactory(dbName);
        using var isolatedClient = isolatedFactory.CreateClient();

        using (var scope = isolatedFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            db.Database.EnsureCreated();
        }

        var payload = new PostSeedWebhookRequest("post-seed", null, null);
        var response = await isolatedClient.PostAsJsonAsync("/api/post-seed-webhook", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PostSeedWebhookResponse>(JsonOptions);
        body.ShouldNotBeNull();
        body!.Received.ShouldBeTrue();
        body.SeedDataDetected.ShouldBeFalse();
    }

    [Test]
    public async Task Should_Return400_When_PostSeedWebhook_EventMissing()
    {
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            db.Database.EnsureCreated();
        }

        var payload = new PostSeedWebhookRequest("", "corr", null);
        var response = await _client!.PostAsJsonAsync("/api/post-seed-webhook", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return415Or400_When_PostSeedWebhook_InvalidOrMalformedBody()
    {
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            db.Database.EnsureCreated();
        }

        using var content = new StringContent("{not-json", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/api/post-seed-webhook", content);
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task Should_Return429_When_PostSeedWebhook_ExceedsRateLimit()
    {
        const string dbName = "post-seed-webhook-ratelimit";
        var cs = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        await using var hold = new SqliteConnection(cs);
        await hold.OpenAsync();
        await using var rateLimitedFactory = new RateLimitedApiWebApplicationFactory(cs);
        using var httpClient = rateLimitedFactory.CreateClient();

        using (var scope = rateLimitedFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            db.Database.EnsureCreated();
        }

        var payload = new PostSeedWebhookRequest("post-seed", "rate-limit", null);
        (await httpClient.PostAsJsonAsync("/api/post-seed-webhook", payload)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await httpClient.PostAsJsonAsync("/api/post-seed-webhook", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_PostSeedWebhook_And_KeyRequired()
    {
        const string dbName = "post-seed-webhook-apikey";
        var cs = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        await using var hold = new SqliteConnection(cs);
        await hold.OpenAsync();
        await using var keyFactory = new ApiKeyProtectedSqliteWebApplicationFactory(cs);

        using (var scope = keyFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            db.Database.EnsureCreated();
            db.Add(new Employee("key-test", "Key", "Test", "key@t.test"));
            db.SaveChanges();
        }

        using var noKeyClient = keyFactory.CreateClient();
        var unauthorized = await noKeyClient.PostAsJsonAsync(
            "/api/post-seed-webhook",
            new PostSeedWebhookRequest("post-seed", null, null));
        unauthorized.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKeyClient = keyFactory.CreateClient();
        withKeyClient.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, ApiKeyProtectedWebApplicationFactory.TestApiKey);
        var ok = await withKeyClient.PostAsJsonAsync(
            "/api/post-seed-webhook",
            new PostSeedWebhookRequest("post-seed", null, null));
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ok.Content.ReadFromJsonAsync<PostSeedWebhookResponse>(JsonOptions);
        body.ShouldNotBeNull();
        body!.SeedDataDetected.ShouldBeTrue();
    }
}
