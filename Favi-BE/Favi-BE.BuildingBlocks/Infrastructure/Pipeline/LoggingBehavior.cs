using MediatR;
using Microsoft.Extensions.Logging;

namespace Favi_BE.BuildingBlocks.Infrastructure.Pipeline;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling request: {RequestType}", typeof(TRequest).Name);

        var response = await next();

        _logger.LogInformation("Handled request: {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
