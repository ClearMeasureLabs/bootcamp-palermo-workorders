using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HelloEndpointTests : IntegratedTestBase
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_GetHello_ReturnOkWithMessage()
    {
        // Act
        var response = await _client!.GetAsync("/api/hello");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<HelloResponse>();
        result.ShouldNotBeNull();
        result.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_GetHello_ReturnJsonContentType()
    {
        // Act
        var response = await _client!.GetAsync("/api/hello");

        // Assert
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    private record HelloResponse(string Message);
}
