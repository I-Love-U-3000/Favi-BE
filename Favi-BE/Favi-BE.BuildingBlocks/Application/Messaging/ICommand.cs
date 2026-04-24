using MediatR;

namespace Favi_BE.BuildingBlocks.Application.Messaging;

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

public interface ICommand : IRequest
{
}
