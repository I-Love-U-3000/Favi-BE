using MediatR;

namespace Favi_BE.BuildingBlocks.Application.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
