using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Favi_BE.BuildingBlocks.Infrastructure.Pipeline;

public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        _logger.LogInformation("Request {RequestType} executed in {ElapsedMs}ms", typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
