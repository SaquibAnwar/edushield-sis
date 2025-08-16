using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EduShield.Core.Exceptions;

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

                var (status, title, details) = ex switch
                {
                    ValidationException validationEx => (
                        StatusCodes.Status400BadRequest, 
                        "Validation failed", 
                        GetValidationErrorDetails(validationEx)
                    ),
                    FeeNotFoundException feeNotFoundEx => (
                        StatusCodes.Status404NotFound, 
                        "Fee not found", 
                        new { feeId = feeNotFoundEx.FeeId }
                    ),
                    StudentNotFoundException studentNotFoundEx => (
                        StatusCodes.Status404NotFound, 
                        "Student not found", 
                        new { studentId = studentNotFoundEx.StudentId }
                    ),
                    PaymentNotFoundException paymentNotFoundEx => (
                        StatusCodes.Status404NotFound, 
                        "Payment not found", 
                        new { paymentId = paymentNotFoundEx.PaymentId }
                    ),
                    InvalidPaymentAmountException invalidPaymentEx => (
                        StatusCodes.Status400BadRequest, 
                        "Invalid payment amount", 
                        new { 
                            feeId = invalidPaymentEx.FeeId,
                            paymentAmount = invalidPaymentEx.PaymentAmount,
                            outstandingAmount = invalidPaymentEx.OutstandingAmount
                        }
                    ),
                    FeeBusinessRuleException businessRuleEx => (
                        StatusCodes.Status409Conflict, 
                        "Business rule violation", 
                        new { 
                            businessRule = businessRuleEx.BusinessRule,
                            feeId = businessRuleEx.FeeId
                        }
                    ),
                    FeeValidationException feeValidationEx => (
                        StatusCodes.Status400BadRequest, 
                        "Fee validation failed", 
                        new { validationErrors = feeValidationEx.ValidationErrors }
                    ),
                    ArgumentException => (
                        StatusCodes.Status400BadRequest, 
                        "Invalid argument", 
                        (object?)null
                    ),
                    InvalidOperationException => (
                        StatusCodes.Status409Conflict, 
                        "Invalid operation", 
                        (object?)null
                    ),
                    KeyNotFoundException => (
                        StatusCodes.Status404NotFound, 
                        "Resource not found", 
                        (object?)null
                    ),
                    _ => (
                        StatusCodes.Status500InternalServerError, 
                        "Unexpected error", 
                        (object?)null
                    )
                };

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = ex?.Message,
                    Instance = ctx.Request.Path
                };
                problem.Extensions["traceId"] = traceId;
                
                if (details != null)
                {
                    problem.Extensions["details"] = details;
                }
                ctx.Response.ContentType = "application/problem+json";
                ctx.Response.StatusCode = status;
                await ctx.Response.WriteAsJsonAsync(problem);
            });
        });
    }

    private static object GetValidationErrorDetails(ValidationException validationException)
    {
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );
        
        return new { validationErrors = errors };
    }
}




