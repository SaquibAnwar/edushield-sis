using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EduShield.Api.Infra;

public static class ErrorHandlingExtensions
{
    public static void UseGlobalProblemDetails(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errApp =>
        {
            errApp.Run(async ctx =>
            {
                var feature = ctx.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;
                var traceId = ctx.TraceIdentifier;

                var (status, title) = ex switch
                {
                    ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
                    KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                    _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
                };

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = ex?.Message,
                    Instance = ctx.Request.Path
                };
                problem.Extensions["traceId"] = traceId;
                ctx.Response.ContentType = "application/problem+json";
                ctx.Response.StatusCode = status;
                await ctx.Response.WriteAsJsonAsync(problem);
            });
        });
    }
}



