using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.API.Middleware
{
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
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            if (exception is BusinessRuleValidationException ruleException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var ruleResponse = new
                {
                    statusCode = 400,
                    code = ruleException.BrokenRule?.GetType().Name ?? "BUSINESS_RULE_VIOLATION",
                    message = ruleException.Message,
                    details = ruleException.BrokenRule?.GetType().FullName ?? string.Empty
                };

                var json = JsonSerializer.Serialize(ruleResponse);
                await context.Response.WriteAsync(json);
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var response = new
            {
                statusCode = 500,
                code = "INTERNAL_SERVER_ERROR",
                message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.",
                details = exception.Message
            };

            var genericJson = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(genericJson);
        }
    }
}
