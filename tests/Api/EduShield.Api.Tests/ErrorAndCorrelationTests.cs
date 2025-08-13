using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace EduShield.Api.Tests;

[TestFixture]
public class ErrorAndCorrelationTests
{
    private CustomWebAppFactory _factory = default!;
    private HttpClient _client = default!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _factory = new CustomWebAppFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Correlation_Echoed_And_ProblemDetails_Has_TraceId()
    {
        // Hit unknown student to trigger 404 via controller returning NotFound
        var cid = Guid.NewGuid().ToString("N");
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/student/{Guid.NewGuid()}");
        req.Headers.Add("X-Correlation-Id", cid);
        var res = await _client.SendAsync(req);
        Assert.That(res.Headers.Contains("X-Correlation-Id"), Is.True);
        Assert.That(res.Headers.GetValues("X-Correlation-Id").First(), Is.EqualTo(cid));
    }

    [Test]
    public async Task Validation_BadRequest_Returns_ProblemDetails()
    {
        var res = await _client.PostAsync("/api/student", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(res.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"));
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Status, Is.EqualTo(400));
        Assert.That(problem.Extensions.ContainsKey("traceId"), Is.True);
    }
}



