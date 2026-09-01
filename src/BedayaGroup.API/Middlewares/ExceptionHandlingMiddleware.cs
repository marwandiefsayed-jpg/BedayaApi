using System.Net;
using System.Text.Json;
using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Models;
using FluentValidation;

namespace BedayaGroup.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = ApiResponse.FailureResult("حدث خطأ أثناء معالجة الطلب");

        switch (exception)
        {
            case ValidationException valException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errorDict = valException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key.Length > 0 ? char.ToLowerInvariant(g.Key[0]) + g.Key[1..] : g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                response = ApiResponse.FailureResult("برجاء مراجعة البيانات المدخلة", errorDict);
                break;

            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = ApiResponse.FailureResult(notFoundEx.Message);
                break;

            case BusinessRuleException businessEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = ApiResponse.FailureResult(businessEx.Message);
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = ApiResponse.FailureResult("غير مصرح لك بالوصول لهذا المورد");
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = ApiResponse.FailureResult("حدث خطأ داخلي في الخادم. يرجى التواصل مع الدعم الفني.");
                break;
        }

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }
}
