using Favi_BE.BuildingBlocks.Application.Data;
using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.BuildingBlocks.Application.Events;
using Favi_BE.BuildingBlocks.Application;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.BuildingBlocks.Infrastructure.Pipeline;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IBuildingBlocksDbContext _dbContext;
    private readonly IDomainEventsDispatcher _domainEventsDispatcher;
    private readonly IExecutionContextAccessor _executionContextAccessor;

    public TransactionBehavior(
        IBuildingBlocksDbContext dbContext,
        IDomainEventsDispatcher domainEventsDispatcher,
        IExecutionContextAccessor executionContextAccessor)
    {
        _dbContext = dbContext;
        _domainEventsDispatcher = domainEventsDispatcher;
        _executionContextAccessor = executionContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommand && request is not ICommand<TResponse>)
        {
            return await next();
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async (ct) =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var response = await next();

                // Dispatch domain events (Phase A + Phase B) before committing
                await _domainEventsDispatcher.DispatchEventsAsync(_executionContextAccessor.CorrelationId, null, ct);

                await transaction.CommitAsync(ct);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }, cancellationToken);
    }
}
