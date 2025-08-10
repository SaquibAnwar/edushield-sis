using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduShield.Api.Tests;

[TestFixture]
public class StudentQueryEndpointsTests
{
    private CustomWebAppFactory _factory = default!;
    private HttpClient _client = default!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _factory = new CustomWebAppFactory();
        _client = _factory.CreateClient();

        // seed
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduShieldDbContext>();
        db.Database.EnsureCreated();
        var existing = db.Students.Count();
        var toAdd = Math.Max(0, 10 - existing);
        for (int i = 0; i < toAdd; i++)
        {
            db.Students.Add(new Student
            {
                Id = Guid.NewGuid(),
                FirstName = $"S{existing + i}",
                LastName = "Test",
                Email = $"s{existing + i}@example.com",
                PhoneNumber = "000",
                DateOfBirth = new DateTime(2000, 1, 1),
                Address = "A",
                EnrollmentDate = DateTime.UtcNow.Date,
                Gender = Gender.M,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        if (toAdd > 0) db.SaveChanges();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task CursorPagination_Works()
    {
        var resp1 = await _client.GetFromJsonAsync<Page>("/v1/students?limit=3");
        Assert.That(resp1, Is.Not.Null);
        Assert.That(resp1!.items.Count, Is.EqualTo(3));
        Assert.That(resp1.nextCursor, Is.Not.Null);

        var resp2 = await _client.GetFromJsonAsync<Page>($"/v1/students?limit=3&after={Uri.EscapeDataString(resp1.nextCursor!)}");
        Assert.That(resp2, Is.Not.Null);
        Assert.That(resp2!.items.Count, Is.EqualTo(3));
        Assert.That(resp2.items.First().Id, Is.Not.EqualTo(resp1.items.First().Id));
    }

    [Test]
    public async Task ETag_Cached_Get_304_On_IfNoneMatch()
    {
        var page = await _client.GetFromJsonAsync<Page>("/v1/students?limit=1");
        var id = page!.items.First().Id;

        var first = await _client.GetAsync($"/v1/students/{id}");
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var etag = first.Headers.ETag?.Tag;
        Assert.That(etag, Is.Not.Null);

        var req = new HttpRequestMessage(HttpMethod.Get, $"/v1/students/{id}");
        req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag!, true));
        var second = await _client.SendAsync(req);
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
    }

    private sealed class Page
    {
        public List<Item> items { get; set; } = new();
        public string? nextCursor { get; set; }
        public int count { get; set; }
    }
    private sealed class Item
    {
        public Guid Id { get; set; }
    }
}


