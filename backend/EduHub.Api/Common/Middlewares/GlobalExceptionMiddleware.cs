using System.Net;
using System.Text.Json;
using EduHub.Api.Common.Exceptions;
using EduHub.Api.Common.Models;

namespace EduHub.Api.Common.Middlewares;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ApiResponse();
        int statusCode;

        if (exception is AppException appEx)
        {
            statusCode = appEx.StatusCode;
            response.Message = appEx.Message;
            response.Errors = appEx.Errors;
            logger.LogWarning("AppException [{StatusCode}]: {Message}", statusCode, appEx.Message);
        }
        else
        {
            statusCode = (int)HttpStatusCode.InternalServerError;
            response.Message = "Đã xảy ra lỗi hệ thống nội bộ. Vui lòng thử lại sau.";
            logger.LogError(exception, "Unhandled Exception: {Message}", exception.Message);
        }

        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
    }
}

