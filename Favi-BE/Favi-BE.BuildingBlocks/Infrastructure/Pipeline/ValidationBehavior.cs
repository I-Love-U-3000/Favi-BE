using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Favi_BE.BuildingBlocks.Infrastructure.Pipeline;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            context,
            results,
            validateAllProperties: true);

        if (!isValid)
        {
            var error = string.Join("; ", results.Select(r => r.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
            throw new ValidationException($"Validation failed for {typeof(TRequest).Name}: {error}");
        }

        return await next();
    }
}
