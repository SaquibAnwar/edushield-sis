using System.Text;
using AutoMapper;
using EduShield.Api.Infra;
using EduShield.Core.Data;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Endpoints;

public static class StudentQueryEndpoints
{
    public static IEndpointRouteBuilder MapStudentQueryEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/v1/students");

        grp.MapGet("/", async (
            int? limit,
            string? after,
            string? name,
            string? email,
            EduShieldDbContext db,
            IMapper mapper,
            CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 50, 1, 200);
            Guid? afterId = string.IsNullOrWhiteSpace(after) ? null : DecodeCursor(after);

            IQueryable<Student> q = db.Students.AsNoTracking().OrderBy(s => s.Id);
            if (afterId is not null) q = q.Where(s => s.Id.CompareTo(afterId.Value) > 0);
            if (!string.IsNullOrWhiteSpace(name)) q = q.Where(s => (s.FirstName + " " + s.LastName).ToLower().Contains(name.ToLower()));
            if (!string.IsNullOrWhiteSpace(email)) q = q.Where(s => s.Email.ToLower().Contains(email.ToLower()));

            var items = await q.Take(take + 1).ToListAsync(ct);
            var hasMore = items.Count > take;
            if (hasMore) items.RemoveAt(items.Count - 1);

            var dtos = items.Select(mapper.Map<StudentDto>).ToList();
            var nextCursor = hasMore ? EncodeCursor(items[^1].Id) : null;
            return Results.Ok(new { items = dtos, nextCursor, count = dtos.Count });
        });

        grp.MapGet("/{id:guid}", async (
            Guid id,
            EduShieldDbContext db,
            IMapper mapper,
            ICacheService cache,
            HttpRequest req,
            HttpResponse res,
            CancellationToken ct) =>
        {
            var key = CacheKeys.Student(id);
            var cached = await cache.GetAsync<StudentDto>(key, ct);
            if (cached is null)
            {
                var entity = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
                if (entity is null) return Results.NotFound();
                cached = mapper.Map<StudentDto>(entity);
                await cache.SetAsync(key, cached, TimeSpan.FromMinutes(15), ct);
            }

            var etag = ETagHelper.Compute($"{id}:{cached.UpdatedAt:O}");
            res.Headers.ETag = etag;
            var ifNoneMatch = req.Headers.IfNoneMatch.ToString();
            if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, etag, StringComparison.Ordinal))
                return Results.StatusCode(StatusCodes.Status304NotModified);

            return Results.Ok(cached);
        });

        return app;

        static string EncodeCursor(Guid id) => Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString()));
        static Guid DecodeCursor(string c) => Guid.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(c)));
    }
}



